import type { SolicitorListing } from '../types';

export type LeadTier = 'Hot' | 'Strong' | 'Watch' | 'Enrich';

export function formatRating(value: number | null): string {
  return value === null ? 'Not rated' : `${value.toFixed(1)} / 5`;
}

export function formatReviewCount(value: number | null): string {
  return value === null ? 'No reviews' : `${value.toLocaleString()} reviews`;
}

export function formatTime(value: string): string {
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: 'medium',
    timeStyle: 'short'
  }).format(new Date(value));
}

export function formatCompactTime(value: string): string {
  return new Intl.DateTimeFormat(undefined, {
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  }).format(new Date(value));
}

export function formatFailureError(value: string): string {
  if (/solicitors\.com returned (?:http )?(?:3\d\d|404)/i.test(value) || /moved permanently/i.test(value)) {
    return 'No results were returned for this location. Check that it is a supported UK town or city.';
  }

  if (/solicitors\.com returned (?:http )?429/i.test(value)) {
    return 'The search provider is rate limiting requests. Try again later.';
  }

  if (/solicitors\.com returned (?:http )?5\d\d/i.test(value)) {
    return 'The search provider is temporarily unavailable. Try again later.';
  }

  return value;
}

export function hasMissingContact(listing: SolicitorListing): boolean {
  return !listing.contactDetails.phoneNumber || !listing.contactDetails.websiteUrl;
}

export function isHighRating(listing: SolicitorListing): boolean {
  return (listing.rating.score ?? 0) >= 4.5 && (listing.rating.reviewCount ?? 0) >= 50;
}

export function getLeadScore(listing: SolicitorListing, isNew = false): number {
  const ratingScore = listing.rating.score === null ? 20 : listing.rating.score * 12;
  const reviewScore = Math.min(18, Math.log10((listing.rating.reviewCount ?? 0) + 1) * 6);
  const contactScore =
    (listing.contactDetails.phoneNumber ? 8 : 0) +
    (listing.contactDetails.websiteUrl ? 8 : listing.contactDetails.contactFormUrl ? 5 : 0) +
    (listing.contactDetails.profileUrl ? 3 : 0);
  const trustScore = Math.min(8, listing.qualityMarks.length * 4) + (listing.isFeatured ? 5 : 0);
  const freshnessScore = isNew ? 8 : 0;

  return Math.min(100, Math.round(ratingScore + reviewScore + contactScore + trustScore + freshnessScore));
}

export function getLeadTier(score: number, listing?: SolicitorListing): LeadTier {
  if (listing && !listing.contactDetails.phoneNumber && !listing.contactDetails.websiteUrl) {
    return 'Enrich';
  }

  if (score >= 82) {
    return 'Hot';
  }

  if (score >= 66) {
    return 'Strong';
  }

  if (score >= 46) {
    return 'Watch';
  }

  return 'Enrich';
}

export function getListingBadges(listing: SolicitorListing, isNew: boolean): string[] {
  const badges: string[] = [];

  if (isNew) {
    badges.push('New');
  }

  if (listing.isFeatured) {
    badges.push('Featured');
  }

  if (listing.qualityMarks.length > 0) {
    badges.push('Quality mark');
  }

  if (!listing.contactDetails.websiteUrl) {
    badges.push('Missing website');
  }

  if (!listing.contactDetails.phoneNumber) {
    badges.push('Missing phone');
  }

  return badges;
}

export function csvValue(value: string | number | null | undefined): string {
  const text = value === null || value === undefined ? '' : String(value);
  return `"${text.replace(/"/g, '""')}"`;
}
