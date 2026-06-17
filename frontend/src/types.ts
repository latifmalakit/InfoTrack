export interface SearchRunRequest {
  locations: string[];
  compareWithPreviousRun: boolean;
}

export interface SearchRunReport {
  runId: string;
  startedAtUtc: string;
  completedAtUtc: string;
  summary: SearchSummary;
  byLocation: LocationReport[];
  listings: SolicitorListing[];
  newListings: SolicitorListing[];
  duplicateFirms: DuplicateFirmReport[];
  failures: LocationSearchFailure[];
}

export interface SearchRunListItem {
  runId: string;
  startedAtUtc: string;
  completedAtUtc: string;
  totalListings: number;
  locationsSearched: number;
  failedLocations: number;
}

export interface SearchSummary {
  totalListings: number;
  newListings: number;
  locationsSearched: number;
  failedLocations: number;
  listingsMissingPhone: number;
  listingsMissingWebsite: number;
  averageRating: number | null;
}

export interface LocationReport {
  location: string;
  listingCount: number;
  newListingCount: number;
  missingPhoneCount: number;
  missingWebsiteCount: number;
  averageRating: number | null;
}

export interface SolicitorListing {
  externalKey: string;
  name: string;
  location: string;
  contactDetails: ContactDetails;
  description: string | null;
  rating: Rating;
  qualityMarks: string[];
  isFeatured: boolean;
}

export interface ContactDetails {
  address: string | null;
  phoneNumber: string | null;
  websiteUrl: string | null;
  contactFormUrl: string | null;
  profileUrl: string | null;
}

export interface Rating {
  score: number | null;
  reviewCount: number | null;
}

export interface DuplicateFirmReport {
  name: string;
  count: number;
  locations: string[];
}

export interface LocationSearchFailure {
  location: string;
  error: string;
}
