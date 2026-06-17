<script setup lang="ts">
import { computed } from 'vue';
import type { SearchRunReport, SolicitorListing } from '../types';
import { formatFailureError, formatRating, formatReviewCount, getLeadScore, getLeadTier } from '../utils/report';

const props = defineProps<{
  report: SearchRunReport;
}>();

const newListingKeys = computed(() => new Set(props.report.newListings.map((listing) => listing.externalKey)));

const maxLocationCount = computed(() => {
  return Math.max(1, ...props.report.byLocation.map((item) => item.listingCount));
});

const topPriorityListings = computed(() => {
  return [...props.report.listings]
    .map((listing) => ({
      listing,
      score: getLeadScore(listing, newListingKeys.value.has(listing.externalKey))
    }))
    .sort((left, right) => right.score - left.score)
    .slice(0, 5);
});

const topRatedListings = computed(() => {
  return [...props.report.listings]
    .filter((listing) => listing.rating.score !== null)
    .sort((left, right) => {
      const scoreComparison = (right.rating.score ?? 0) - (left.rating.score ?? 0);
      if (scoreComparison !== 0) {
        return scoreComparison;
      }

      return (right.rating.reviewCount ?? 0) - (left.rating.reviewCount ?? 0);
    })
    .slice(0, 5);
});

function isNewListing(listing: SolicitorListing): boolean {
  return newListingKeys.value.has(listing.externalKey);
}
</script>

<template>
  <section class="overview-stack">
    <section class="metrics-grid" aria-label="Search summary">
      <article>
        <span>Total listings</span>
        <strong>{{ report.summary.totalListings }}</strong>
      </article>
      <article>
        <span>New listings</span>
        <strong>{{ report.summary.newListings }}</strong>
      </article>
      <article>
        <span>Average rating</span>
        <strong>{{ formatRating(report.summary.averageRating) }}</strong>
      </article>
      <article>
        <span>Missing website</span>
        <strong>{{ report.summary.listingsMissingWebsite }}</strong>
      </article>
    </section>

    <section v-if="report.failures.length" class="failure-strip">
      <strong>Partial results</strong>
      <span v-for="failure in report.failures" :key="failure.location">
        {{ failure.location }}: {{ formatFailureError(failure.error) }}
      </span>
    </section>

    <section class="insight-grid">
      <article class="insight-panel">
        <div class="panel-heading">
          <h3>Priority leads</h3>
          <span>{{ topPriorityListings.length }}</span>
        </div>
        <ol class="rank-list score-list">
          <li v-for="item in topPriorityListings" :key="item.listing.externalKey">
            <div>
              <strong>{{ item.listing.name }}</strong>
              <span>{{ item.listing.location }} · {{ getLeadTier(item.score, item.listing) }}</span>
            </div>
            <em>{{ item.score }}</em>
          </li>
        </ol>
      </article>

      <article class="insight-panel">
        <div class="panel-heading">
          <h3>Locations</h3>
          <span>{{ report.byLocation.length }}</span>
        </div>
        <div class="location-bars">
          <div v-for="item in report.byLocation" :key="item.location" class="bar-row">
            <div class="bar-label">
              <span>{{ item.location }}</span>
              <strong>{{ item.listingCount }} / {{ item.newListingCount }} new</strong>
            </div>
            <div class="bar-track">
              <span :style="{ width: `${(item.listingCount / maxLocationCount) * 100}%` }"></span>
            </div>
          </div>
        </div>
      </article>

      <article class="insight-panel">
        <div class="panel-heading">
          <h3>Top rated</h3>
          <span>{{ topRatedListings.length }}</span>
        </div>
        <ol class="rank-list">
          <li v-for="listing in topRatedListings" :key="listing.externalKey">
            <div>
              <strong>{{ listing.name }}</strong>
              <span>{{ formatReviewCount(listing.rating.reviewCount) }}</span>
            </div>
            <em>{{ formatRating(listing.rating.score) }}</em>
          </li>
        </ol>
      </article>

      <article class="insight-panel">
        <div class="panel-heading">
          <h3>New this run</h3>
          <span>{{ report.newListings.length }}</span>
        </div>
        <ul class="compact-list">
          <li v-for="listing in report.newListings.slice(0, 6)" :key="listing.externalKey">
            <div>
              <strong>{{ listing.name }}</strong>
              <span>{{ listing.location }}</span>
            </div>
            <em>{{ isNewListing(listing) ? 'New' : '' }}</em>
          </li>
        </ul>
      </article>

      <article class="insight-panel">
        <div class="panel-heading">
          <h3>Duplicate firms</h3>
          <span>{{ report.duplicateFirms.length }}</span>
        </div>
        <ul class="compact-list">
          <li v-for="firm in report.duplicateFirms.slice(0, 6)" :key="firm.name">
            <div>
              <strong>{{ firm.name }}</strong>
              <span>{{ firm.locations.join(', ') }}</span>
            </div>
            <em>{{ firm.count }}</em>
          </li>
        </ul>
      </article>

      <article class="insight-panel">
        <div class="panel-heading">
          <h3>Contact gaps</h3>
          <span>{{ report.summary.listingsMissingPhone + report.summary.listingsMissingWebsite }}</span>
        </div>
        <div class="quality-grid">
          <div>
            <span>Missing phone</span>
            <strong>{{ report.summary.listingsMissingPhone }}</strong>
          </div>
          <div>
            <span>Missing website</span>
            <strong>{{ report.summary.listingsMissingWebsite }}</strong>
          </div>
          <div>
            <span>Failed locations</span>
            <strong>{{ report.summary.failedLocations }}</strong>
          </div>
        </div>
      </article>
    </section>
  </section>
</template>
