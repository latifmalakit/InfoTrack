<script setup lang="ts">
import { computed } from 'vue';
import type { SearchRunReport } from '../types';
import { formatFailureError } from '../utils/report';

const props = defineProps<{
  report: SearchRunReport;
}>();

const noWebsiteCount = computed(() => {
  return props.report.listings.filter((listing) => !listing.contactDetails.websiteUrl).length;
});

const noContactFormCount = computed(() => {
  return props.report.listings.filter((listing) => !listing.contactDetails.contactFormUrl).length;
});
</script>

<template>
  <section class="issues-stack">
    <section class="issue-grid">
      <article>
        <span>Failed locations</span>
        <strong>{{ report.summary.failedLocations }}</strong>
      </article>
      <article>
        <span>Missing phone</span>
        <strong>{{ report.summary.listingsMissingPhone }}</strong>
      </article>
      <article>
        <span>Missing website</span>
        <strong>{{ noWebsiteCount }}</strong>
      </article>
      <article>
        <span>No contact form</span>
        <strong>{{ noContactFormCount }}</strong>
      </article>
    </section>

    <section v-if="report.failures.length" class="failure-strip">
      <strong>Failed locations</strong>
      <span v-for="failure in report.failures" :key="failure.location">
        {{ failure.location }}: {{ formatFailureError(failure.error) }}
      </span>
    </section>

    <section class="insight-panel">
      <div class="panel-heading">
        <h3>Duplicate firms</h3>
        <span>{{ report.duplicateFirms.length }}</span>
      </div>
      <ul class="compact-list">
        <li v-for="firm in report.duplicateFirms" :key="firm.name">
          <div>
            <strong>{{ firm.name }}</strong>
            <span>{{ firm.locations.join(', ') }}</span>
          </div>
          <em>{{ firm.count }} offices</em>
        </li>
      </ul>
    </section>
  </section>
</template>
