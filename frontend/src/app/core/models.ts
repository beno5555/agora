// Types from docs/architecture/api.md. Amounts are strings of base units.

export interface ApiResponse<T> {
  succeeded: boolean;
  message: string;
  data: T;
}

export interface ConfigDto {
  cluster: string;
  rpcUrl: string;
  programId: string;
  mint: string;
  mintDecimals: number;
  feeBps: number | null;
  deliveryWindows: { key: string; seconds: number }[];
  defaultDeliveryWindow: string;
}

export interface MinimalUserDto { wallet: string; displayName: string | null }
export interface FullUserDto { wallet: string; displayName: string | null; discord: string | null; createdAt: string }
export interface ReputationDto { dealsCompleted: number; dealsDisputed: number; dealsUndelivered: number; volume: string }
export interface StandardUserDto {
  wallet: string;
  displayName: string | null;
  discord: string | null;
  reputation: ReputationDto;
  activeListings: MinimalListingDto[];
}

export interface GameDto { id: number; slug: string; name: string; iconUrl: string | null; activeListings: number }

export type ListingType = 'Service' | 'Item';

export interface ListingTermsDto {
  gameId: number;
  type: ListingType;
  title: string;
  description: string;
  price: string;
  milestones: number;
  unitLabel: string;
  deliveryWindow: string;
}

export interface MinimalListingDto {
  id: string;
  title: string;
  gameSlug: string;
  gameName: string;
  type: ListingType;
  price: string;
  milestones: number;
  unitLabel: string;
  seller: MinimalUserDto;
}

export interface StandardListingDto extends MinimalListingDto {
  description: string;
  deliveryWindow: string;
  isActive: boolean;
  createdAt: string;
  sellerReputation: ReputationDto;
  depositPreview: { buyerDeposit: string; sellerDeposit: string } | null;
}

export type DealStatus =
  | 'Draft' | 'AwaitingSeller' | 'Active' | 'Delivered' | 'Disputed'
  | 'Completed' | 'Split' | 'Undelivered' | 'SellerRefunded' | 'Cancelled' | 'Expired';

export type DealAction =
  | 'Accept' | 'Cancel' | 'MarkDelivered' | 'Confirm' | 'RefundUndelivered'
  | 'RaiseDispute' | 'ProposeSplit' | 'AcceptSplit' | 'SellerRefund';

export interface MinimalDealDto {
  id: string;
  title: string;
  status: DealStatus;
  price: string;
  role: 'buyer' | 'seller';
  createdAt: string;
}

export interface FullDealDto extends MinimalDealDto {
  dealPda: string;
  onchainDealId: string;
  listingId: string;
  gameName: string;
  unitLabel: string;
  buyerWallet: string;
  sellerWallet: string;
  buyerNote: string | null;
  milestones: number;
  milestonesReleased: number;
  buyerDeposit: string;
  sellerDeposit: string;
  deliveryWindowSeconds: number;
  acceptedAt: string | null;
  deliveredAt: string | null;
  deliveryDeadline: string | null;
  splitOffer: { by: string; buyerAmount: string } | null;
  lastSignature: string | null;
  allowedActions: DealAction[];
}

export interface DealActionDto { action: DealAction; parts?: number; buyerAmount?: string }
export interface DealTransactionDto { deal: FullDealDto; transaction: string }

export type NotificationKind =
  | 'DealCreated' | 'DealAccepted' | 'DealDelivered' | 'PartsConfirmed' | 'DealDisputed'
  | 'SplitProposed' | 'DealClosed' | 'DeadlineSoon' | 'RefundAvailable';

export interface NotificationDto { id: number; dealId: string | null; kind: NotificationKind; createdAt: string; isRead: boolean }
