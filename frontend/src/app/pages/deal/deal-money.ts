import { DealAction, DealStatus, FullDealDto } from '../../core/models';
import { formatAmount } from '../../shared/amount.pipe';
import { unitCount, unitName } from '../../shared/unit';
import { MoneyMoment } from '../../shared/vault';

export const FINAL_STATUSES: DealStatus[] = [
  'Completed',
  'Split',
  'Undelivered',
  'SellerRefunded',
  'Cancelled',
  'Expired',
];
const LOCKED_STATUSES: DealStatus[] = ['AwaitingSeller', 'Active', 'Delivered', 'Disputed'];

/** Actions that can be the page's one primary button, most important first (DESIGN.md §5.4). */
const PRIMARY_ORDER: DealAction[] = [
  'Accept',
  'Confirm',
  'MarkDelivered',
  'AcceptSplit',
  'RefundUndelivered',
  'ClaimPayment',
];

export type OutcomeKind =
  | 'cancel'
  | 'decline'
  | 'confirm-one'
  | 'confirm-all'
  | 'deadline'
  | 'claim'
  | 'dispute'
  | 'split'
  | 'refund';

/** One "What happens if…" row, already worded from the viewer's side. */
export interface Outcome {
  kind: OutcomeKind;
  label: string;
  result: string;
}

/** One "Who can move this money" line. */
export interface KeyLine {
  icon: 'key' | 'clock';
  who: string;
  what: string;
}

/** The page's one primary action, or null when nothing is waiting on the viewer (their move). */
/** "18:30" today, "Oct 14, 18:30" on another day. */
function timeText(iso: string | null): string {
  if (!iso) return '';
  const date = new Date(iso);
  const sameDay = date.toDateString() === new Date().toDateString();
  return new Intl.DateTimeFormat(
    undefined,
    sameDay ? { timeStyle: 'short' } : { dateStyle: 'medium', timeStyle: 'short' },
  ).format(date);
}

export function primaryAction(actions: DealAction[]): DealAction | null {
  return PRIMARY_ORDER.find((a) => actions.includes(a)) ?? null;
}

/** The money event between two versions of a deal, for the vault to animate; null if none. */
export function moneyMoment(prev: FullDealDto, next: FullDealDto): MoneyMoment | null {
  if (prev.status === next.status) return null;
  if (FINAL_STATUSES.includes(next.status)) return 'close';
  const locked =
    (prev.status === 'Draft' && next.status === 'AwaitingSeller') ||
    (prev.status === 'AwaitingSeller' && next.status === 'Active');
  return locked ? 'lock' : null;
}

/** What the other side just did, for a toast when polling picks it up; null if nothing worth saying. */
export function changeNotice(prev: FullDealDto, next: DealMoney): string | null {
  const d = next.deal;
  const them = next.isBuyer ? 'The seller' : 'The buyer';
  if (prev.status !== d.status) {
    switch (d.status) {
      case 'Active':
        return prev.status === 'AwaitingSeller' && next.isBuyer
          ? `The seller accepted. Their ${next.money(next.sellerDepositTerms)} deposit is locked.`
          : null;
      case 'Delivered':
        return next.isBuyer ? 'The seller marked it delivered. Check, then confirm.' : null;
      case 'Disputed':
        return `${them} disagreed. The money stays locked until you both accept the same split.`;
      case 'Completed':
        return 'The deal is complete.';
      default:
        return FINAL_STATUSES.includes(d.status) ? 'This deal is closed.' : null;
    }
  }
  const mine = next.isBuyer ? d.buyerWallet : d.sellerWallet;
  if (
    d.splitOffer &&
    d.splitOffer.by !== mine &&
    d.splitOffer.buyerAmount !== prev.splitOffer?.buyerAmount
  )
    return `${them} offered a split.`;
  const confirmed = d.milestonesReleased - prev.milestonesReleased;
  if (confirmed > 0 && !next.isBuyer)
    return `The buyer confirmed ${next.units(confirmed)}. The money is on its way to you.`;
  return null;
}

/**
 * Every amount and sentence the deal page shows, worked out from the deal and the viewer's role.
 * Amounts follow frontend.md: remaining = price − price/milestones × released; the fee is taken
 * only from what the seller is paid.
 */
export class DealMoney {
  constructor(
    readonly deal: FullDealDto,
    private readonly decimals: number,
    private readonly feeBps: bigint,
    /** True once the delivery deadline has passed (from the shared clock). */
    private readonly pastDeadline: boolean,
  ) {}

  get isBuyer(): boolean {
    return this.deal.role === 'buyer';
  }
  get isFinal(): boolean {
    return FINAL_STATUSES.includes(this.deal.status);
  }
  get isLocked(): boolean {
    return LOCKED_STATUSES.includes(this.deal.status);
  }
  get feePercent(): number {
    return Number(this.feeBps) / 100;
  }

  get price(): bigint {
    return BigInt(this.deal.price);
  }
  get perPart(): bigint {
    return this.price / BigInt(this.deal.milestones);
  }
  get released(): bigint {
    return this.perPart * BigInt(this.deal.milestonesReleased);
  }
  get remaining(): bigint {
    return this.price - this.released;
  }
  get partsLeft(): number {
    return this.deal.milestones - this.deal.milestonesReleased;
  }
  get buyerDeposit(): bigint {
    return BigInt(this.deal.buyerDeposit);
  }
  /** The seller's deposit as agreed; it is only in the vault once they accept. */
  get sellerDepositTerms(): bigint {
    return BigInt(this.deal.sellerDeposit);
  }
  get sellerHasLocked(): boolean {
    return this.deal.status !== 'AwaitingSeller' && this.deal.status !== 'Draft';
  }
  get sellerDeposit(): bigint {
    return this.sellerHasLocked ? this.sellerDepositTerms : 0n;
  }
  get locked(): bigint {
    return this.isLocked ? this.remaining + this.buyerDeposit + this.sellerDeposit : 0n;
  }

  minusFee(amount: bigint): bigint {
    return amount - (amount * this.feeBps) / 10_000n;
  }
  /** What confirming `parts` sends to the seller before the fee; the last part also carries the rounding. */
  confirmAmount(parts: number): bigint {
    return parts >= this.partsLeft ? this.remaining : this.perPart * BigInt(parts);
  }
  /** What the viewer receives, deposit included, if they accept the other side's split offer. */
  get splitYouGet(): bigint {
    const offer = this.deal.splitOffer;
    if (!offer) return 0n;
    const toBuyer = BigInt(offer.buyerAmount);
    return this.isBuyer
      ? toBuyer + this.buyerDeposit
      : this.minusFee(this.remaining - toBuyer) + this.sellerDeposit;
  }

  /** Dollars with two decimals (DESIGN.md §7), rounded half up to the cent. */
  money(amount: bigint): string {
    const cent = 10n ** BigInt(Math.max(this.decimals - 2, 0));
    const rounded = ((amount + cent / 2n) / cent) * cent;
    return `$${formatAmount(rounded, this.decimals)}`;
  }
  units(count: number): string {
    return unitCount(this.deal.unit, count);
  }
  get unit(): string {
    return unitName(this.deal.unit);
  }
  get unitPlural(): string {
    return unitName(this.deal.unit, 2);
  }

  /** "the 24-hour clock" wording for the delivery window. */
  get windowText(): string {
    const s = this.deal.deliveryWindowSeconds;
    if (s % 86_400 === 0 && s > 86_400) return `${s / 86_400}-day`;
    if (s % 3_600 === 0) return `${s / 3_600}-hour`;
    return `${Math.round(s / 60)}-minute`;
  }
  get deadlineText(): string {
    return timeText(this.deal.deliveryDeadline);
  }
  /** When the buyer's time to check a delivery ends. */
  get reviewDeadlineText(): string {
    return timeText(this.deal.reviewDeadline);
  }

  /** The action that gets the one primary button, if any. */
  primaryOf(actions: DealAction[]): DealAction | null {
    return primaryAction(actions);
  }

  /** The line under the title: what state the deal is in and whose move it is (DESIGN.md §7). */
  get statusLine(): string {
    const b = this.isBuyer;
    switch (this.deal.status) {
      case 'Draft':
        return b
          ? 'Approve the payment in Phantom to fund this deal.'
          : "The buyer hasn't funded this deal yet.";
      case 'AwaitingSeller': {
        const paid = this.money(this.remaining + this.buyerDeposit);
        return b
          ? `Your ${paid} is locked. Waiting for the seller to accept.`
          : `A buyer locked ${paid}. Accept to lock your ${this.money(this.sellerDepositTerms)} deposit and start the ${this.windowText} clock.`;
      }
      case 'Active':
        if (this.pastDeadline)
          return b
            ? 'The seller missed the deadline. You can take your refund plus their deposit.'
            : 'You missed the deadline. The buyer can take a refund and your deposit.';
        return b
          ? `The seller is delivering. Confirm each ${this.unit} when you get it.`
          : `Deliver by ${this.deadlineText}, then mark it delivered.`;
      case 'Delivered':
        return b
          ? `The seller says it's delivered. Check, then confirm or disagree by ${this.reviewDeadlineText}.`
          : `Waiting for the buyer to confirm. If they don't answer by ${this.reviewDeadlineText}, you can claim your payment.`;
      case 'Disputed':
        return 'You disagree. The money stays locked until you both accept the same split.';
      case 'Completed':
        return b
          ? 'Done. The seller was paid and both deposits are back.'
          : `Done. You were paid ${this.money(this.minusFee(this.price))} and your deposit is back.`;
      case 'Undelivered':
        return b
          ? "The seller didn't deliver in time. You got your money back plus their deposit."
          : "You didn't deliver in time. The buyer got a refund and your deposit.";
      case 'Split':
        return 'Closed with the split you both accepted. Nothing is locked.';
      case 'SellerRefunded':
        return b
          ? 'The seller refunded you. Nothing is locked.'
          : 'You refunded the buyer. Nothing is locked.';
      case 'Cancelled':
        return b
          ? 'You cancelled before the seller accepted. Nothing is locked.'
          : 'The buyer cancelled before you accepted. Nothing is locked.';
      default:
        return 'Closed. It expired before it was funded.';
    }
  }

  /** Replaces the key list once the deal is closed: where the money went. */
  get closedSentence(): string {
    switch (this.deal.status) {
      case 'Completed':
        return `Closed. The seller was paid ${this.money(this.minusFee(this.price))} and both deposits went back.`;
      case 'Undelivered':
        return `Closed. The buyer got ${this.money(this.remaining + this.buyerDeposit + this.sellerDepositTerms)} back, including the seller's deposit.`;
      case 'Split':
        return 'Closed. The money was split the way both sides agreed, and both deposits went back.';
      case 'SellerRefunded':
        return `Closed. The buyer got ${this.money(this.remaining + this.buyerDeposit)} back${this.deal.acceptedAt ? ", and the seller's deposit went back to the seller." : '.'}`;
      case 'Cancelled':
        return `Closed. The buyer got ${this.money(this.remaining + this.buyerDeposit)} back.`;
      default:
        return 'Closed before it was funded. Nothing was locked.';
    }
  }

  /** Who can still move the money: the viewer from their allowed actions, the other side by the rules. */
  get keyLines(): KeyLine[] {
    const d = this.deal;
    const b = this.isBuyer;
    const you = b ? 'You (buyer)' : 'You (seller)';
    const other = b ? 'Seller' : 'Buyer';
    const lines: KeyLine[] = [{ icon: 'key', who: you, what: this.yourKey(d.allowedActions) }];

    let theirs = '';
    switch (d.status) {
      case 'AwaitingSeller':
        theirs = b
          ? `accept and lock their ${this.money(this.sellerDepositTerms)} deposit, or refund you`
          : 'cancel and take their money back';
        break;
      case 'Active':
      case 'Delivered':
        theirs = b ? 'refund you' : `confirm ${this.unitPlural}, or disagree`;
        break;
      case 'Disputed':
        theirs = 'accept your offer, or offer their own split';
        break;
    }
    if (theirs) lines.push({ icon: 'key', who: other, what: theirs });

    if (d.status === 'Active' && d.deliveryDeadline)
      lines.push({
        icon: 'clock',
        who: `The deadline, ${this.deadlineText}`,
        what: b
          ? 'if nothing is delivered, you can take your refund plus their deposit'
          : "if you haven't delivered, the buyer can take a refund plus your deposit",
      });
    if (d.status === 'Delivered' && d.reviewDeadline)
      lines.push({
        icon: 'clock',
        who: `The review time, until ${this.reviewDeadlineText}`,
        what: b
          ? 'if you neither confirm nor disagree by then, the seller can claim the payment'
          : 'if the buyer neither confirms nor disagrees by then, you can claim the payment',
      });
    return lines;
  }

  private yourKey(actions: DealAction[]): string {
    const phrases: Partial<Record<DealAction, string>> = {
      Accept: `accept and lock your ${this.money(this.sellerDepositTerms)} deposit`,
      Cancel: `cancel and take your ${this.money(this.remaining + this.buyerDeposit)} back`,
      Confirm: `confirm ${this.unitPlural}`,
      RefundUndelivered: 'take your refund plus their deposit',
      ClaimPayment: 'claim the payment',
      AcceptSplit: 'accept their offer',
      ProposeSplit: 'offer a split',
      SellerRefund: 'refund the buyer',
      RaiseDispute: 'disagree',
    };
    const list = actions.map((a) => phrases[a]).filter((p): p is string => !!p);
    if (list.length === 0) return 'nothing right now';
    if (list.length === 1) return list[0];
    return `${list.slice(0, -1).join(', ')}, or ${list[list.length - 1]}`;
  }

  /** One line under each button saying what it does, in dollars. */
  consequence(action: DealAction, parts = 1): string {
    const fee = this.feePercent;
    switch (action) {
      case 'Accept':
        return `Locks your ${this.money(this.sellerDepositTerms)} deposit and starts the ${this.windowText} clock.`;
      case 'Confirm': {
        const all = parts >= this.partsLeft;
        return `Sends ${this.money(this.confirmAmount(parts))} to the seller, minus the ${fee}% fee.${all ? ' Both deposits go back and the deal closes.' : ''}`;
      }
      case 'MarkDelivered':
        return 'Tells the buyer it has arrived. No money moves until they confirm.';
      case 'AcceptSplit':
        return `You get ${this.money(this.splitYouGet)}, your deposit included. The deal closes.`;
      case 'RefundUndelivered':
        return `You get ${this.money(this.remaining + this.buyerDeposit + this.sellerDeposit)}: your ${this.money(this.remaining)}, your ${this.money(this.buyerDeposit)} deposit and the seller's ${this.money(this.sellerDeposit)} deposit.`;
      case 'Cancel':
        return `You get your ${this.money(this.remaining + this.buyerDeposit)} back. The deal closes.`;
      case 'SellerRefund':
        return this.sellerHasLocked
          ? `The buyer gets ${this.money(this.remaining + this.buyerDeposit)} back, and your ${this.money(this.sellerDeposit)} deposit comes back to you.`
          : `The buyer gets ${this.money(this.remaining + this.buyerDeposit)} back. Nothing of yours was locked.`;
      case 'RaiseDispute':
        return 'Locks everything until you both accept the same split.';
      case 'ClaimPayment':
        return `The buyer didn't answer in time. You get ${this.money(this.minusFee(this.remaining))} (after the ${fee}% fee) and your ${this.money(this.sellerDeposit)} deposit back; the buyer's deposit goes back to them.`;
      default:
        return '';
    }
  }

  /** What a split offer of `toBuyer` pays each side, deposits included. */
  splitResult(toBuyer: bigint): string {
    const buyer = toBuyer + this.buyerDeposit;
    const seller = this.minusFee(this.remaining - toBuyer) + this.sellerDeposit;
    return this.isBuyer
      ? `You would get ${this.money(buyer)} and the seller ${this.money(seller)}, deposits included.`
      : `The buyer would get ${this.money(buyer)} and you ${this.money(seller)}, deposits included.`;
  }

  /** The current offer, from the viewer's side. */
  get offerLine(): string | null {
    const offer = this.deal.splitOffer;
    if (!offer) return null;
    const toBuyer = BigInt(offer.buyerAmount);
    const toSeller = this.remaining - toBuyer;
    const mine = offer.by === (this.isBuyer ? this.deal.buyerWallet : this.deal.sellerWallet);
    const you = this.money(this.isBuyer ? toBuyer : toSeller);
    const them = this.money(this.isBuyer ? toSeller : toBuyer);
    const left = this.money(this.remaining);
    const fee = `The seller's share has the ${this.feePercent}% fee taken out.`;
    if (mine) return `You offered: you get ${you}, they get ${them} of the ${left} left. ${fee}`;
    const who = this.isBuyer ? 'The seller' : 'The buyer';
    return `${who} offered: you get ${you}, they get ${them} of the ${left} left. ${fee}`;
  }

  /** The reachable ways this deal can end, worded from the viewer's side. */
  get outcomes(): Outcome[] {
    const d = this.deal;
    const b = this.isBuyer;
    const nothing = 'nothing';
    const row = (kind: OutcomeKind, label: string, buyer: string, seller: string): Outcome => ({
      kind,
      label,
      result: b ? `You: ${buyer} · Seller: ${seller}` : `You: ${seller} · Buyer: ${buyer}`,
    });
    const rows: Outcome[] = [];
    const back = this.money(this.remaining + this.buyerDeposit);

    if (d.status === 'AwaitingSeller') {
      rows.push(row('cancel', b ? 'You cancel' : 'The buyer cancels', back, nothing));
      rows.push(row('decline', b ? 'The seller declines' : 'You decline', back, nothing));
      return rows;
    }
    if (!this.isLocked) return rows;

    if (this.partsLeft > 1)
      rows.push(
        row(
          'confirm-one',
          b ? `You confirm 1 ${this.unit}` : `The buyer confirms 1 ${this.unit}`,
          nothing,
          this.money(this.minusFee(this.perPart)),
        ),
      );
    rows.push(
      row(
        'confirm-all',
        this.partsLeft > 1
          ? b
            ? `You confirm all ${this.partsLeft} left`
            : `The buyer confirms all ${this.partsLeft} left`
          : b
            ? 'You confirm'
            : 'The buyer confirms',
        `${this.money(this.buyerDeposit)} deposit back`,
        `${this.money(this.minusFee(this.remaining))} + ${this.money(this.sellerDeposit)} deposit back`,
      ),
    );
    if (d.status === 'Active' && d.deliveryDeadline)
      rows.push(
        row(
          'deadline',
          b ? `The seller misses ${this.deadlineText}` : `You miss ${this.deadlineText}`,
          this.money(this.remaining + this.buyerDeposit + this.sellerDeposit),
          nothing,
        ),
      );
    if (d.status === 'Delivered' && d.reviewDeadline)
      rows.push(
        row(
          'claim',
          b
            ? `You don't answer by ${this.reviewDeadlineText}`
            : `The buyer doesn't answer by ${this.reviewDeadlineText}`,
          `${this.money(this.buyerDeposit)} deposit back`,
          `${this.money(this.minusFee(this.remaining))} + ${this.money(this.sellerDeposit)} deposit back`,
        ),
      );
    if (d.allowedActions.includes('RaiseDispute'))
      rows.push({
        kind: 'dispute',
        label: 'You disagree',
        result: 'Everything stays locked until you both accept the same split',
      });
    if (d.splitOffer) {
      const toBuyer = BigInt(d.splitOffer.buyerAmount);
      rows.push(
        row(
          'split',
          'The current offer is accepted',
          this.money(toBuyer + this.buyerDeposit),
          this.money(this.minusFee(this.remaining - toBuyer) + this.sellerDeposit),
        ),
      );
    } else if (d.status === 'Disputed') {
      rows.push({
        kind: 'split',
        label: 'You agree on a split',
        result: 'Each side gets their share plus their deposit',
      });
    }
    rows.push(
      row(
        'refund',
        b ? 'The seller refunds you' : 'You refund the buyer',
        back,
        `${this.money(this.sellerDeposit)} deposit back`,
      ),
    );
    return rows;
  }

  /** Which outcome row the viewer's primary button leads to. */
  nextStepKind(primary: DealAction | null, parts: number): OutcomeKind | null {
    switch (primary) {
      case 'Confirm':
        return parts >= this.partsLeft ? 'confirm-all' : 'confirm-one';
      case 'RefundUndelivered':
        return 'deadline';
      case 'ClaimPayment':
        return 'claim';
      case 'AcceptSplit':
        return 'split';
      default:
        return null;
    }
  }
}
