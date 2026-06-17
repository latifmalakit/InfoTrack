<script setup lang="ts">
import { computed, ref } from 'vue';
import type { SearchRunReport, SolicitorListing } from '../types';
import {
  csvValue,
  formatRating,
  formatReviewCount,
  getLeadScore,
  getLeadTier,
  getListingBadges,
  hasMissingContact,
  isHighRating
} from '../utils/report';

type TableMode = 'all' | 'new' | 'issues';
type StatusFilter = 'all' | 'hot' | 'new' | 'missingContact' | 'missingWebsite' | 'featured' | 'qualityMark';
type SortKey = 'score' | 'reviews' | 'rating' | 'name' | 'location' | 'contact';

const props = withDefaults(
  defineProps<{
    report: SearchRunReport;
    mode?: TableMode;
    title?: string;
  }>(),
  {
    mode: 'all',
    title: 'Listings'
  }
);

const emit = defineEmits<{
  openListing: [listing: SolicitorListing];
}>();

const searchTerm = ref('');
const locationFilter = ref('All');
const statusFilter = ref<StatusFilter>('all');
const sortKey = ref<SortKey>('score');

const newListingKeys = computed(() => new Set(props.report.newListings.map((listing) => listing.externalKey)));

const sourceListings = computed(() => {
  if (props.mode === 'new') {
    return props.report.listings.filter((listing) => isNewListing(listing));
  }

  if (props.mode === 'issues') {
    return props.report.listings.filter((listing) => hasMissingContact(listing));
  }

  return props.report.listings;
});

const locationOptions = computed(() => {
  const names = new Set(sourceListings.value.map((listing) => listing.location));
  return ['All', ...Array.from(names).sort((a, b) => a.localeCompare(b))];
});

const filteredListings = computed(() => {
  const search = searchTerm.value.trim().toLowerCase();

  return sourceListings.value
    .filter((listing) => {
      if (locationFilter.value !== 'All' && listing.location !== locationFilter.value) {
        return false;
      }

      if (!matchesStatus(listing, statusFilter.value)) {
        return false;
      }

      if (!search) {
        return true;
      }

      return [
        listing.name,
        listing.location,
        listing.contactDetails.address,
        listing.contactDetails.phoneNumber,
        listing.contactDetails.websiteUrl,
        listing.description
      ]
        .filter(Boolean)
        .some((value) => String(value).toLowerCase().includes(search));
    })
    .sort(compareListings);
});

function isNewListing(listing: SolicitorListing): boolean {
  return newListingKeys.value.has(listing.externalKey);
}

function matchesStatus(listing: SolicitorListing, filter: StatusFilter): boolean {
  if (filter === 'all') {
    return true;
  }

  if (filter === 'hot') {
    return getLeadTier(getLeadScore(listing, isNewListing(listing)), listing) === 'Hot';
  }

  if (filter === 'new') {
    return isNewListing(listing);
  }

  if (filter === 'missingContact') {
    return hasMissingContact(listing);
  }

  if (filter === 'missingWebsite') {
    return !listing.contactDetails.websiteUrl;
  }

  if (filter === 'featured') {
    return listing.isFeatured;
  }

  return listing.qualityMarks.length > 0;
}

function compareListings(left: SolicitorListing, right: SolicitorListing): number {
  if (sortKey.value === 'score') {
    return getLeadScore(right, isNewListing(right)) - getLeadScore(left, isNewListing(left));
  }

  if (sortKey.value === 'reviews') {
    return (right.rating.reviewCount ?? -1) - (left.rating.reviewCount ?? -1);
  }

  if (sortKey.value === 'rating') {
    return (right.rating.score ?? -1) - (left.rating.score ?? -1);
  }

  if (sortKey.value === 'contact') {
    return Number(hasMissingContact(right)) - Number(hasMissingContact(left));
  }

  return String(left[sortKey.value]).localeCompare(String(right[sortKey.value]));
}

function exportCsv(): void {
  const header = [
    'Name',
    'Location',
    'Lead score',
    'Tier',
    'Rating',
    'Reviews',
    'Phone',
    'Website',
    'Contact form',
    'Profile',
    'Address',
    'New listing'
  ];

  const rows = filteredListings.value.map((listing) => {
    const isNew = isNewListing(listing);
    const score = getLeadScore(listing, isNew);

    return [
      listing.name,
      listing.location,
      score,
      getLeadTier(score, listing),
      listing.rating.score,
      listing.rating.reviewCount,
      listing.contactDetails.phoneNumber,
      listing.contactDetails.websiteUrl,
      listing.contactDetails.contactFormUrl,
      listing.contactDetails.profileUrl,
      listing.contactDetails.address,
      isNew ? 'yes' : 'no'
    ];
  });

  const csv = [header, ...rows].map((row) => row.map(csvValue).join(',')).join('\n');
  const blob = new Blob([csv], { type: 'text/csv;charset=utf-8' });
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = `infotrack-leads-${props.report.runId.slice(0, 8)}.csv`;
  link.click();
  URL.revokeObjectURL(url);
}
</script>

<template>
  <section class="table-panel">
    <div class="table-header">
      <div>
        <h3>{{ title }}</h3>
        <p>{{ filteredListings.length }} of {{ sourceListings.length }} listings</p>
      </div>
      <button class="secondary compact-action" type="button" :disabled="filteredListings.length === 0" @click="exportCsv">
        Export CSV
      </button>
    </div>

    <div class="filter-grid">
      <label>
        <span>Search</span>
        <input v-model="searchTerm" type="search" placeholder="Firm, address, phone" />
      </label>
      <label>
        <span>Location</span>
        <select v-model="locationFilter">
          <option v-for="option in locationOptions" :key="option" :value="option">{{ option }}</option>
        </select>
      </label>
      <label>
        <span>Status</span>
        <select v-model="statusFilter">
          <option value="all">All</option>
          <option value="hot">Hot leads</option>
          <option value="new">New</option>
          <option value="missingContact">Missing contact</option>
          <option value="missingWebsite">Missing website</option>
          <option value="featured">Featured</option>
          <option value="qualityMark">Quality mark</option>
        </select>
      </label>
      <label>
        <span>Sort</span>
        <select v-model="sortKey">
          <option value="score">Lead score</option>
          <option value="reviews">Reviews</option>
          <option value="rating">Rating</option>
          <option value="name">Name</option>
          <option value="location">Location</option>
          <option value="contact">Contact gaps</option>
        </select>
      </label>
    </div>

    <div class="table-scroll">
      <table>
        <thead>
          <tr>
            <th>Firm</th>
            <th>Score</th>
            <th>Location</th>
            <th>Rating</th>
            <th>Contact</th>
            <th>Action</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="listing in filteredListings" :key="`${listing.externalKey}-${listing.location}`">
            <td class="firm-cell">
              <strong>{{ listing.name }}</strong>
              <span>{{ listing.contactDetails.address ?? 'No address' }}</span>
              <div class="badge-row">
                <span
                  v-for="badge in getListingBadges(listing, isNewListing(listing))"
                  :key="badge"
                  class="status-badge"
                  :class="{ warning: badge.startsWith('Missing'), success: badge === 'New' || badge === 'Quality mark' }"
                >
                  {{ badge }}
                </span>
                <span v-if="isHighRating(listing)" class="status-badge success">High rating</span>
              </div>
            </td>
            <td>
              <div class="score-pill" :data-tier="getLeadTier(getLeadScore(listing, isNewListing(listing)), listing)">
                <strong>{{ getLeadScore(listing, isNewListing(listing)) }}</strong>
                <span>{{ getLeadTier(getLeadScore(listing, isNewListing(listing)), listing) }}</span>
              </div>
            </td>
            <td>{{ listing.location }}</td>
            <td>
              <strong>{{ formatRating(listing.rating.score) }}</strong>
              <span>{{ formatReviewCount(listing.rating.reviewCount) }}</span>
            </td>
            <td>
              <span>{{ listing.contactDetails.phoneNumber ?? 'Missing phone' }}</span>
              <a v-if="listing.contactDetails.websiteUrl" :href="listing.contactDetails.websiteUrl" target="_blank" rel="noreferrer">
                Website
              </a>
              <span v-else>Missing website</span>
            </td>
            <td>
              <button class="text-action" type="button" @click="emit('openListing', listing)">Details</button>
            </td>
          </tr>
        </tbody>
      </table>

      <div v-if="filteredListings.length === 0" class="table-empty">
        <strong>No matching listings</strong>
        <span>Adjust the active filters.</span>
      </div>
    </div>
  </section>
</template>
