<script setup lang="ts">
import { ref } from 'vue';
import { MAX_LOCATION_LENGTH, normalizeLocationInput, validateLocationInput } from '../utils/location';

defineProps<{
  locations: string[];
  isLoading: boolean;
  isResetting: boolean;
}>();

const emit = defineEmits<{
  add: [location: string];
  remove: [location: string];
  reset: [];
  run: [];
}>();

const input = ref('');
const validationMessage = ref<string | null>(null);

function submitLocation(): void {
  const value = normalizeLocationInput(input.value);
  if (!value) {
    validationMessage.value = null;
    return;
  }

  const error = validateLocationInput(value);
  if (error) {
    validationMessage.value = error;
    return;
  }

  emit('add', value);
  input.value = '';
  validationMessage.value = null;
}
</script>

<template>
  <section class="location-section" aria-label="Locations">
    <form class="location-form" @submit.prevent="submitLocation">
      <label for="location">Locations</label>
      <div class="input-row">
        <input
          id="location"
          v-model="input"
          type="text"
          autocomplete="off"
          placeholder="Add a city"
          :maxlength="MAX_LOCATION_LENGTH"
          :aria-invalid="validationMessage ? 'true' : 'false'"
          aria-describedby="location-validation"
          @input="validationMessage = null"
        />
        <button type="submit" :disabled="isLoading">Add</button>
      </div>
      <p id="location-validation" class="field-message" :class="{ 'is-error': validationMessage }">
        {{ validationMessage ?? `${MAX_LOCATION_LENGTH} characters maximum` }}
      </p>
    </form>

    <div class="location-list">
      <button
        v-for="location in locations"
        :key="location"
        class="location-chip"
        type="button"
        :aria-label="`Remove ${location}`"
        :disabled="isLoading"
        @click="emit('remove', location)"
      >
        <span :title="location">{{ location }}</span>
        <strong>x</strong>
      </button>
    </div>

    <div class="control-actions">
      <button class="secondary" type="button" :disabled="isResetting || isLoading" @click="emit('reset')">
        {{ isResetting ? 'Resetting' : 'Reset' }}
      </button>
      <button class="primary" type="button" :disabled="isLoading || locations.length === 0" @click="emit('run')">
        {{ isLoading ? 'Searching' : 'Run Search' }}
      </button>
    </div>
  </section>
</template>
