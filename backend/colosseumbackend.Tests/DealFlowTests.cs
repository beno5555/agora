using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using colosseumbackend.Common;
using colosseumbackend.Dtos;
using Solnet.Wallet;

namespace colosseumbackend.Tests;

/// <summary>
/// End to end through the real API and a local validator running the compiled program:
/// the backend builds unsigned transactions, test wallets sign and send them, sync reads the chain.
/// </summary>
[Collection(SolanaCollection.Name)]
public class DealFlowTests
{
    private const ulong Token = SolanaTestFixture.Token;

    private readonly SolanaTestFixture _fixture;

    public DealFlowTests(SolanaTestFixture fixture)
    {
        _fixture = fixture;
    }

    // ---------- full deal: per-match confirmation ----------
    // New wallets on both sides, so both deposits are the 20% base rate: 3 tokens on a 15-token carry.

    [Fact]
    public async Task DealLifecycle_ConfirmEveryMatch_CompletesAndPaysSellerMinusFee()
    {
        var (seller, sellerClient) = await NewSignedInUserAsync();
        var (buyer,  buyerClient)  = await NewSignedInUserAsync();
        var listing = await CreateCarryListingAsync(sellerClient);
        ulong sellerBefore = await _fixture.TokenBalanceAsync(seller.PublicKey);

        var preview = await buyerClient.GetFromJsonAsync<ApplicationResponse<StandardListingDto>>($"api/listings/{listing.Id}", TestJson.Options);
        var deal    = await CreateAndFundDealAsync(buyerClient, buyer, listing.Id);
        deal        = await ActAsync(sellerClient, seller, deal.Id, new DealActionDto { Action = DealAction.Accept });
        deal        = await ActAsync(buyerClient, buyer, deal.Id, new DealActionDto { Action = DealAction.Confirm, Parts = 1 });

        Assert.Equal((3 * Token).ToString(), preview!.Data!.DepositPreview!.BuyerDeposit);
        Assert.Equal((3 * Token).ToString(), deal.SellerDeposit);
        Assert.Equal(DealStatus.Active, deal.Status);
        Assert.Equal(1, deal.MilestonesReleased);

        deal = await ActAsync(buyerClient, buyer, deal.Id, new DealActionDto { Action = DealAction.Confirm, Parts = 2 });

        Assert.Equal(DealStatus.Completed, deal.Status);
        Assert.Equal(3, deal.MilestonesReleased);
        Assert.Empty(deal.AllowedActions);
        // 15 tokens minus the 2% fee; the seller's deposit came back.
        Assert.Equal(sellerBefore + 147 * Token / 10, await _fixture.TokenBalanceAsync(seller.PublicKey));

        var notifications = await sellerClient.GetFromJsonAsync<ListResponse<NotificationDto>>("api/me/notifications", TestJson.Options);
        Assert.Contains(notifications!.Data, notification => notification.Kind == NotificationKind.DealCreated);
        Assert.Contains(notifications.Data, notification => notification.Kind == NotificationKind.DealClosed);
    }

    // ---------- disagreement ----------

    [Fact]
    public async Task Dispute_SplitAcceptedByOtherParty_ClosesAsSplit()
    {
        var (seller, sellerClient) = await NewSignedInUserAsync();
        var (buyer,  buyerClient)  = await NewSignedInUserAsync();
        var listing = await CreateCarryListingAsync(sellerClient);
        ulong buyerBefore = await _fixture.TokenBalanceAsync(buyer.PublicKey);

        var deal = await CreateAndFundDealAsync(buyerClient, buyer, listing.Id);
        deal     = await ActAsync(sellerClient, seller, deal.Id, new DealActionDto { Action = DealAction.Accept });
        deal     = await ActAsync(sellerClient, seller, deal.Id, new DealActionDto { Action = DealAction.MarkDelivered });
        deal     = await ActAsync(buyerClient, buyer, deal.Id, new DealActionDto { Action = DealAction.RaiseDispute });
        deal     = await ActAsync(sellerClient, seller, deal.Id, new DealActionDto { Action = DealAction.ProposeSplit, BuyerAmount = (10 * Token).ToString() });

        var sellerView = await sellerClient.GetFromJsonAsync<ApplicationResponse<FullDealDto>>($"api/deals/{deal.Id}", TestJson.Options);
        Assert.DoesNotContain(DealAction.AcceptSplit, sellerView!.Data!.AllowedActions); // can't accept own offer

        // The open disagreement counts against both parties, each in their role on this deal.
        Assert.Equal(1u, (await ReputationAsync(buyerClient, buyer)).AsBuyer.OpenDisputes);
        Assert.Equal(1u, (await ReputationAsync(buyerClient, seller)).AsSeller.OpenDisputes);

        // Times read back from the database must carry the UTC marker, or browsers parse them as local time.
        string sellerJson = await sellerClient.GetStringAsync($"api/deals/{deal.Id}");
        Assert.Matches("\"acceptedAt\":\"[^\"]+Z\"", sellerJson);

        deal = await ActAsync(buyerClient, buyer, deal.Id, new DealActionDto { Action = DealAction.AcceptSplit });

        Assert.Equal(DealStatus.Split, deal.Status);
        Assert.Equal(buyerBefore - 5 * Token, await _fixture.TokenBalanceAsync(buyer.PublicKey));

        var buyerRecord = await ReputationAsync(buyerClient, buyer);
        Assert.Equal(0u, buyerRecord.AsBuyer.OpenDisputes);
        Assert.Equal(1u, buyerRecord.AsBuyer.DealsDisputed);
        Assert.Equal(0u, buyerRecord.AsSeller.DealsCompleted); // buying doesn't build a seller record
    }

    // ---------- missed delivery deadline (the demo flow, with the devnet-only 2-minute window) ----------

    [Fact]
    public async Task MissedDeadline_BuyerTakesRefund_GetsPriceBackPlusSellerDeposit()
    {
        var (seller, sellerClient) = await NewSignedInUserAsync();
        var (buyer,  buyerClient)  = await NewSignedInUserAsync();
        var config = await buyerClient.GetFromJsonAsync<ApplicationResponse<ConfigDto>>("api/config", TestJson.Options);
        var listing = await CreateCarryListingAsync(sellerClient, deliveryWindow: "2m");
        ulong buyerBefore = await _fixture.TokenBalanceAsync(buyer.PublicKey);

        var deal = await CreateAndFundDealAsync(buyerClient, buyer, listing.Id);
        deal     = await ActAsync(sellerClient, seller, deal.Id, new DealActionDto { Action = DealAction.Accept });

        Assert.Contains(config!.Data!.DeliveryWindows, window => window.Key == "2m");
        Assert.Equal("2m", listing.DeliveryWindow);
        Assert.DoesNotContain(DealAction.RefundUndelivered, deal.AllowedActions);

        // The seller never delivers. Wait out the deadline plus the clock grace.
        var wait = deal.DeliveryDeadline!.Value + Constants.ChainClockGrace + TimeSpan.FromSeconds(5) - DateTime.UtcNow;
        await Task.Delay(wait > TimeSpan.Zero ? wait : TimeSpan.Zero);
        var buyerView = await buyerClient.GetFromJsonAsync<ApplicationResponse<FullDealDto>>($"api/deals/{deal.Id}", TestJson.Options);
        Assert.Contains(DealAction.RefundUndelivered, buyerView!.Data!.AllowedActions);

        deal = await ActAsync(buyerClient, buyer, deal.Id, new DealActionDto { Action = DealAction.RefundUndelivered });

        Assert.Equal(DealStatus.Undelivered, deal.Status);
        // Price and own deposit back, plus the seller's 3-token deposit.
        Assert.Equal(buyerBefore + 3 * Token, await _fixture.TokenBalanceAsync(buyer.PublicKey));
    }

    // ---------- access and validation ----------

    [Fact]
    public async Task GetDeal_ByWalletThatIsNotAParty_ReturnsNotFound()
    {
        var (_,     sellerClient)   = await NewSignedInUserAsync();
        var (buyer, buyerClient)    = await NewSignedInUserAsync();
        var (_,     strangerClient) = await NewSignedInUserAsync();
        var listing = await CreateCarryListingAsync(sellerClient);
        var deal    = await CreateAndFundDealAsync(buyerClient, buyer, listing.Id);

        var response = await strangerClient.GetAsync($"api/deals/{deal.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task BuildTransaction_ForActionNotAllowedInState_ReturnsBadRequest()
    {
        var (_,     sellerClient) = await NewSignedInUserAsync();
        var (buyer, buyerClient)  = await NewSignedInUserAsync();
        var listing = await CreateCarryListingAsync(sellerClient);
        var deal    = await CreateAndFundDealAsync(buyerClient, buyer, listing.Id);

        // Only the seller can accept; the buyer can only cancel while the deal awaits the seller.
        var response = await buyerClient.PostAsJsonAsync($"api/deals/{deal.Id}/tx", new DealActionDto { Action = DealAction.Accept }, TestJson.Options);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateDeal_OnOwnListing_ReturnsBadRequest()
    {
        var (_, sellerClient) = await NewSignedInUserAsync();
        var listing = await CreateCarryListingAsync(sellerClient);

        var response = await sellerClient.PostAsJsonAsync("api/deals", new CreateDealDto { ListingId = listing.Id }, TestJson.Options);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateListing_PriceNotDivisibleByMilestones_ReturnsBadRequest()
    {
        var (_, sellerClient) = await NewSignedInUserAsync();

        var response = await sellerClient.PostAsJsonAsync("api/listings", CarryListing(price: 10 * Token + 1), TestJson.Options);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetMyListings_WithPausedListing_ReturnsBothAndOnlyMine()
    {
        var (_, sellerClient) = await NewSignedInUserAsync();
        var (_, otherClient)  = await NewSignedInUserAsync();
        var paused  = await CreateCarryListingAsync(sellerClient);
        var active  = await CreateCarryListingAsync(sellerClient);
        var others  = await CreateCarryListingAsync(otherClient);
        (await sellerClient.PostAsync($"api/listings/{paused.Id}/pause", null)).EnsureSuccessStatusCode();

        var mine = await sellerClient.GetFromJsonAsync<ListResponse<FullListingDto>>("api/me/listings", TestJson.Options);

        Assert.Equal(2, mine!.Data.Count);
        Assert.False(mine.Data.Single(listing => listing.Id == paused.Id).IsActive);
        Assert.True(mine.Data.Single(listing => listing.Id == active.Id).IsActive);
        Assert.DoesNotContain(mine.Data, listing => listing.Id == others.Id);
    }

    [Fact]
    public async Task CreateListing_UnknownDeliveryWindow_ReturnsBadRequest()
    {
        var (_, sellerClient) = await NewSignedInUserAsync();

        var response = await sellerClient.PostAsJsonAsync("api/listings", CarryListing(deliveryWindow: "5m"), TestJson.Options);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("dse")]
    [InlineData(99)]
    public async Task CreateListing_UnitNotInList_ReturnsBadRequest(object unit)
    {
        var (_, sellerClient) = await NewSignedInUserAsync();
        var body = JsonSerializer.SerializeToNode(CarryListing(), TestJson.Options)!;
        body["unit"] = JsonSerializer.SerializeToNode(unit);

        var response = await sellerClient.PostAsJsonAsync("api/listings", body, TestJson.Options);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateListing_WithoutUnit_SavesListingWithNoUnit()
    {
        var (_, sellerClient) = await NewSignedInUserAsync();
        var terms = CarryListing();
        terms.Unit = null;

        var response = await sellerClient.PostAsJsonAsync("api/listings", terms, TestJson.Options);
        var listing  = (await response.Content.ReadFromJsonAsync<ApplicationResponse<StandardListingDto>>(TestJson.Options))!.Data!;

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Null(listing.Unit);
    }

    [Fact]
    public async Task CreateListing_WithoutSession_ReturnsUnauthorized()
    {
        var client = _fixture.CreateClient();

        var response = await client.PostAsJsonAsync("api/listings", CarryListing(), TestJson.Options);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---------- sign-in ----------

    [Fact]
    public async Task VerifySignIn_SignatureFromDifferentWallet_ReturnsUnauthorized()
    {
        var client   = _fixture.CreateClient();
        var wallet   = new Account();
        var impostor = new Account();
        var nonce    = await client.GetFromJsonAsync<ApplicationResponse<NonceResponseDto>>($"api/auth/nonce?wallet={wallet.PublicKey.Key}", TestJson.Options);
        string message   = nonce!.Data!.Message;
        string signature = Solnet.Wallet.Utilities.Encoders.Base58.EncodeData(impostor.Sign(System.Text.Encoding.UTF8.GetBytes(message)));

        var response = await client.PostAsJsonAsync("api/auth/verify",
            new VerifySignInDto { Wallet = wallet.PublicKey.Key, Message = message, Signature = signature }, TestJson.Options);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task VerifySignIn_ReusingAnAcceptedMessage_ReturnsUnauthorized()
    {
        var client = _fixture.CreateClient();
        var wallet = new Account();
        var nonce  = await client.GetFromJsonAsync<ApplicationResponse<NonceResponseDto>>($"api/auth/nonce?wallet={wallet.PublicKey.Key}", TestJson.Options);
        string message   = nonce!.Data!.Message;
        string signature = Solnet.Wallet.Utilities.Encoders.Base58.EncodeData(wallet.Sign(System.Text.Encoding.UTF8.GetBytes(message)));
        var body = new VerifySignInDto { Wallet = wallet.PublicKey.Key, Message = message, Signature = signature };

        var first  = await client.PostAsJsonAsync("api/auth/verify", body, TestJson.Options);
        var replay = await client.PostAsJsonAsync("api/auth/verify", body, TestJson.Options);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);
    }

    #region Helpers
    private async Task<(Account Wallet, HttpClient Client)> NewSignedInUserAsync()
    {
        var wallet = await _fixture.CreateFundedWalletAsync();
        var client = _fixture.CreateClient();
        await _fixture.SignInAsync(client, wallet);
        return (wallet, client);
    }

    private static CreateListingDto CarryListing(ulong price = 15 * Token, string deliveryWindow = "1h")
    {
        return new CreateListingDto
        {
            GameId         = 1, // Fortnite (seeded)
            Type           = ListingType.Service,
            Title          = "Fortnite carry, 3 matches",
            Price          = price.ToString(),
            Milestones     = 3,
            Unit           = ListingUnit.Match,
            DeliveryWindow = deliveryWindow,
        };
    }

    private static async Task<StandardListingDto> CreateCarryListingAsync(HttpClient sellerClient, string deliveryWindow = "1h")
    {
        var response = await sellerClient.PostAsJsonAsync("api/listings", CarryListing(deliveryWindow: deliveryWindow), TestJson.Options);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ApplicationResponse<StandardListingDto>>(TestJson.Options))!.Data!;
    }

    private async Task<FullDealDto> CreateAndFundDealAsync(HttpClient buyerClient, Account buyer, Guid listingId)
    {
        var response = await buyerClient.PostAsJsonAsync("api/deals", new CreateDealDto { ListingId = listingId, BuyerNote = "IGN: tester" }, TestJson.Options);
        response.EnsureSuccessStatusCode();
        var created = (await response.Content.ReadFromJsonAsync<ApplicationResponse<DealTransactionDto>>(TestJson.Options))!.Data!;

        return await SendAndSyncAsync(buyerClient, buyer, created.Deal.Id, created.Transaction);
    }

    private static async Task<WalletReputationDto> ReputationAsync(HttpClient client, Account wallet)
    {
        var profile = await client.GetFromJsonAsync<ApplicationResponse<StandardUserDto>>($"api/users/{wallet.PublicKey}", TestJson.Options);
        return profile!.Data!.Reputation;
    }

    private async Task<FullDealDto> ActAsync(HttpClient client, Account wallet, Guid dealId, DealActionDto action)
    {
        var response = await client.PostAsJsonAsync($"api/deals/{dealId}/tx", action, TestJson.Options);
        response.EnsureSuccessStatusCode();
        var built = (await response.Content.ReadFromJsonAsync<ApplicationResponse<DealTransactionDto>>(TestJson.Options))!.Data!;

        return await SendAndSyncAsync(client, wallet, dealId, built.Transaction);
    }

    private async Task<FullDealDto> SendAndSyncAsync(HttpClient client, Account wallet, Guid dealId, string unsignedTransaction)
    {
        string signature = await _fixture.SignAndSendAsync(unsignedTransaction, wallet);
        var    response  = await client.PostAsJsonAsync($"api/deals/{dealId}/sync", new SyncDealDto { Signature = signature }, TestJson.Options);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<ApplicationResponse<FullDealDto>>(TestJson.Options))!.Data!;
    }
    #endregion
}
