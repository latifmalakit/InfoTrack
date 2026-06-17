<script setup lang="ts">
import type { SearchRunListItem } from '../types';
import { formatCompactTime } from '../utils/report';

defineProps<{
  recentRuns: SearchRunListItem[];
  activeRunId: string | null;
  isRecentLoading: boolean;
  isDisabled: boolean;
  openingRunId: string | null;
}>();

const emit = defineEmits<{
  refresh: [];
  open: [runId: string];
}>();
</script>

<template>
  <section class="history-panel" aria-label="Recent search runs">
    <div class="history-heading">
      <h2>Recent runs</h2>
      <button type="button" :disabled="isRecentLoading || isDisabled" @click="emit('refresh')">
        {{ isRecentLoading ? 'Loading' : 'Refresh' }}
      </button>
    </div>

    <div v-if="recentRuns.length" class="history-list">
      <button
        v-for="run in recentRuns"
        :key="run.runId"
        class="history-item"
        type="button"
        :class="{ active: activeRunId === run.runId }"
        :disabled="openingRunId !== null || isDisabled"
        @click="emit('open', run.runId)"
      >
        <span>{{ formatCompactTime(run.completedAtUtc) }}</span>
        <strong>{{ run.totalListings }} listings</strong>
        <em>{{ run.locationsSearched }} locations, {{ run.failedLocations }} failed</em>
      </button>
    </div>

    <p v-else class="empty-history">{{ isRecentLoading ? 'Loading' : 'No runs' }}</p>
  </section>
</template>
