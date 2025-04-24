<!-- filepath: e:\work\QmtdltTools\web\qmtdlt_tools_vue\src\components\HighlightedText.vue -->
<template>
  <div style="min-height: 200px;" ref="containerRef">
    <p>
      <span>{{ beforeText }}</span>
      <span class="highlight">{{ highlightedText }}</span>
      <span>{{ afterText }}</span>
    </p>
  </div>
</template>

<script setup lang="ts">
import { computed, defineProps,defineEmits ,onMounted, onBeforeUnmount,ref} from 'vue';

const props = defineProps<{
  fullText: string;
  highlightText: string;
}>();
const containerRef = ref<HTMLElement | null>(null)
const beforeText = computed(() => {
  const index = props.fullText.indexOf(props.highlightText);
  return index > -1 ? props.fullText.substring(0, index) : props.fullText;
});

const highlightedText = computed(() => {
  const index = props.fullText.indexOf(props.highlightText);
  return index > -1 ? props.highlightText : '';
});

const afterText = computed(() => {
  const index = props.fullText.indexOf(props.highlightText);
  if (index === -1) {
    return '';
  }
  const startIndex = index + props.highlightText.length;
  return props.fullText.substring(startIndex);
});
const emit = defineEmits<{
  (e: 'phaseSelect', selectedText: string): void
}>()
function handleSelection() {
  const selection = window.getSelection()
  if (
    selection &&
    selection.toString().trim() &&
    containerRef.value &&
    selection.anchorNode &&
    containerRef.value.contains(selection.anchorNode)
  ) {
    emit('phaseSelect', selection.toString())
    selection.removeAllRanges()
  }
}

onMounted(() => {
  document.addEventListener('selectionchange', handleSelection)
})
onBeforeUnmount(() => {
  document.removeEventListener('selectionchange', handleSelection)
})
</script>

<style scoped>
.highlight {
  background-color: yellow;
  font-weight: bold;
}
</style>