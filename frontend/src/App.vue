<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import ListingDetailDrawer from './components/ListingDetailDrawer.vue';
import ListingsTable from './components/ListingsTable.vue';
import LocationSelector from './components/LocationSelector.vue';
import RecentRuns from './components/RecentRuns.vue';
import ReportIssues from './components/ReportIssues.vue';
import ReportOverview from './components/ReportOverview.vue';
import { getDefaultLocations, getRecentSearchRuns, getSearchRun, runSearch } from './services/api';
import type { SearchRunListItem, SearchRunReport, SolicitorListing } from './types';
import { normalizeLocationInput, validateLocationInput } from './utils/location';
import { formatTime } from './utils/report';

type ReportTab = 'overview' | 'leads' | 'new' | 'issues';

const locations = ref<string[]>([]);
const isLoading = ref(false);
const errorMessage = ref<string | null>(null);
const report = ref<SearchRunReport | null>(null);
const isResetting = ref(false);
const recentRuns = ref<SearchRunListItem[]>([]);
const isRecentLoading = ref(false);
const openingRunId = ref<string | null>(null);
const activeTab = ref<ReportTab>('overview');
const selectedListing = ref<SolicitorListing | null>(null);

const newListingKeys = computed(() => {
  return new Set(report.value?.newListings.map((listing) => listing.externalKey) ?? []);
});

const selectedListingIsNew = computed(() => {
  return selectedListing.value ? newListingKeys.value.has(selectedListing.value.externalKey) : false;
});

const reportTabs = computed<Array<{ key: ReportTab; label: string; count: number | null }>>(() => {
  if (!report.value) {
    return [];
  }

  return [
    { key: 'overview', label: 'Overview', count: null },
    { key: 'leads', label: 'Leads', count: report.value.summary.totalListings },
    { key: 'new', label: 'New', count: report.value.summary.newListings },
    {
      key: 'issues',
      label: 'Issues',
      count: report.value.summary.failedLocations + report.value.summary.listingsMissingPhone + report.value.summary.listingsMissingWebsite
    }
  ];
});

onMounted(() => {
  void loadDefaults();
  void loadRecentRuns();
});

async function loadDefaults(): Promise<void> {
  try {
    locations.value = await getDefaultLocations();
  } catch (error) {
    errorMessage.value = error instanceof Error ? error.message : 'Failed to load default locations.';
  }
}

async function loadRecentRuns(): Promise<void> {
  isRecentLoading.value = true;

  try {
    recentRuns.value = await getRecentSearchRuns();
  } catch (error) {
    errorMessage.value = error instanceof Error ? error.message : 'Failed to load recent runs.';
  } finally {
    isRecentLoading.value = false;
  }
}

function addLocation(value: string): void {
  const normalized = normalizeLocationInput(value);
  const validationError = validateLocationInput(normalized);
  if (validationError) {
    errorMessage.value = validationError;
    return;
  }

  if (!locations.value.some((location) => location.toLowerCase() === normalized.toLowerCase())) {
    locations.value = [...locations.value, normalized];
  }
}

function removeLocation(locationToRemove: string): void {
  locations.value = locations.value.filter((location) => location !== locationToRemove);
}

async function resetLocations(): Promise<void> {
  isResetting.value = true;
  errorMessage.value = null;

  try {
    locations.value = await getDefaultLocations();
  } catch (error) {
    errorMessage.value = error instanceof Error ? error.message : 'Failed to reset locations.';
  } finally {
    isResetting.value = false;
  }
}

async function submitSearch(): Promise<void> {
  isLoading.value = true;
  errorMessage.value = null;
  selectedListing.value = null;

  try {
    report.value = await runSearch({
      locations: locations.value,
      compareWithPreviousRun: true
    });
    activeTab.value = 'overview';
    await loadRecentRuns();
  } catch (error) {
    errorMessage.value = error instanceof Error ? error.message : 'Search failed.';
  } finally {
    isLoading.value = false;
  }
}

async function openRecentRun(runId: string): Promise<void> {
  openingRunId.value = runId;
  errorMessage.value = null;
  selectedListing.value = null;

  try {
    report.value = await getSearchRun(runId);
    activeTab.value = 'overview';
  } catch (error) {
    errorMessage.value = error instanceof Error ? error.message : 'Failed to load saved report.';
  } finally {
    openingRunId.value = null;
  }
}

function openListing(listing: SolicitorListing): void {
  selectedListing.value = listing;
}
</script>

<template>
  <main class="shell">
    <section class="workspace">
      <aside class="control-panel">
        <div class="brand-block">
          <span class="brand-mark">IT</span>
          <div>
            <h1>Solicitor Intelligence</h1>
            <p>Conveyancing lead discovery</p>
          </div>
        </div>

        <LocationSelector
          :locations="locations"
          :is-loading="isLoading"
          :is-resetting="isResetting"
          @add="addLocation"
          @remove="removeLocation"
          @reset="resetLocations"
          @run="submitSearch"
        />

        <RecentRuns
          :recent-runs="recentRuns"
          :active-run-id="report?.runId ?? null"
          :is-recent-loading="isRecentLoading"
          :is-disabled="isLoading"
          :opening-run-id="openingRunId"
          @refresh="loadRecentRuns"
          @open="openRecentRun"
        />

        <p v-if="errorMessage" class="error-message">{{ errorMessage }}</p>
      </aside>

      <section class="report-panel">
        <div v-if="isLoading" class="search-state">
          <div class="activity-line">
            <span></span>
          </div>
          <h2>Running solicitor search</h2>
          <p>{{ locations.length }} locations queued</p>
          <div class="skeleton-grid">
            <span></span>
            <span></span>
            <span></span>
            <span></span>
          </div>
        </div>

        <div v-else-if="!report" class="empty-state">
          <span class="eyebrow">Ready</span>
          <h2>Solicitor Intelligence</h2>
          <p>Run a search to build the sales report.</p>
        </div>

        <template v-else>
          <header class="report-header">
            <div>
              <span class="eyebrow">Conveyancing report</span>
              <h2>{{ report.summary.totalListings }} solicitor listings</h2>
              <p>{{ formatTime(report.completedAtUtc) }}</p>
            </div>
            <div class="report-actions">
              <span class="run-id">{{ report.runId.slice(0, 8) }}</span>
              <span class="status-badge success" v-if="report.summary.failedLocations === 0">Complete</span>
              <span class="status-badge warning" v-else>Partial</span>
            </div>
          </header>

          <nav class="report-tabs" aria-label="Report views">
            <button
              v-for="tab in reportTabs"
              :key="tab.key"
              type="button"
              :class="{ active: activeTab === tab.key }"
              @click="activeTab = tab.key"
            >
              <span>{{ tab.label }}</span>
              <strong v-if="tab.count !== null">{{ tab.count }}</strong>
            </button>
          </nav>

          <ReportOverview v-if="activeTab === 'overview'" :report="report" />

          <ListingsTable
            v-else-if="activeTab === 'leads'"
            :report="report"
            title="Lead pipeline"
            @open-listing="openListing"
          />

          <ListingsTable
            v-else-if="activeTab === 'new'"
            :report="report"
            mode="new"
            title="New since previous run"
            @open-listing="openListing"
          />

          <template v-else>
            <ReportIssues :report="report" />
            <ListingsTable
              :report="report"
              mode="issues"
              title="Listings needing enrichment"
              @open-listing="openListing"
            />
          </template>
        </template>
      </section>
    </section>

    <ListingDetailDrawer
      :listing="selectedListing"
      :is-new="selectedListingIsNew"
      @close="selectedListing = null"
    />
  </main>
</template>
