<script setup lang="ts">
import { computed } from 'vue';
import type { SolicitorListing } from '../types';
import { formatRating, formatReviewCount, getLeadScore, getLeadTier } from '../utils/report';

const props = defineProps<{
  listing: SolicitorListing | null;
  isNew: boolean;
}>();

const emit = defineEmits<{
  close: [];
}>();

const score = computed(() => {
  return props.listing ? getLeadScore(props.listing, props.isNew) : 0;
});

const tier = computed(() => {
  return props.listing ? getLeadTier(score.value, props.listing) : 'Watch';
});
</script>

<template>
  <div v-if="listing" class="drawer-backdrop" @click.self="emit('close')">
    <aside class="detail-drawer" aria-label="Listing details">
      <header class="drawer-header">
        <div>
          <span class="eyebrow">{{ listing.location }}</span>
          <h2>{{ listing.name }}</h2>
        </div>
        <button type="button" aria-label="Close details" @click="emit('close')">x</button>
      </header>

      <div class="drawer-score">
        <div class="score-pill large" :data-tier="tier">
          <strong>{{ score }}</strong>
          <span>{{ tier }}</span>
        </div>
        <div>
          <strong>{{ formatRating(listing.rating.score) }}</strong>
          <span>{{ formatReviewCount(listing.rating.reviewCount) }}</span>
        </div>
      </div>

      <section class="detail-section">
        <h3>Contact</h3>
        <dl>
          <div>
            <dt>Phone</dt>
            <dd>{{ listing.contactDetails.phoneNumber ?? 'Missing' }}</dd>
          </div>
          <div>
            <dt>Address</dt>
            <dd>{{ listing.contactDetails.address ?? 'Missing' }}</dd>
          </div>
        </dl>
      </section>

      <section class="detail-section">
        <h3>Links</h3>
        <div class="link-stack">
          <a v-if="listing.contactDetails.websiteUrl" :href="listing.contactDetails.websiteUrl" target="_blank" rel="noreferrer">Website</a>
          <a v-if="listing.contactDetails.contactFormUrl" :href="listing.contactDetails.contactFormUrl" target="_blank" rel="noreferrer">Contact form</a>
          <a v-if="listing.contactDetails.profileUrl" :href="listing.contactDetails.profileUrl" target="_blank" rel="noreferrer">Solicitors.com profile</a>
          <span v-if="!listing.contactDetails.websiteUrl && !listing.contactDetails.contactFormUrl && !listing.contactDetails.profileUrl">No links found</span>
        </div>
      </section>

      <section v-if="listing.description" class="detail-section">
        <h3>Description</h3>
        <p>{{ listing.description }}</p>
      </section>

      <section class="detail-section">
        <h3>Signals</h3>
        <div class="badge-row">
          <span v-if="isNew" class="status-badge success">New</span>
          <span v-if="listing.isFeatured" class="status-badge">Featured</span>
          <span v-for="mark in listing.qualityMarks" :key="mark" class="status-badge success">{{ mark }}</span>
          <span v-if="!listing.qualityMarks.length && !listing.isFeatured && !isNew" class="muted-text">No extra signals</span>
        </div>
      </section>
    </aside>
  </div>
</template>
