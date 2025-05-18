<template>
  <div class="highlightdiv" ref="containerRef">
    <p ref="paragraphRef">
      <span>{{ beforeText }}</span>
      <span class="highlight" ref="highlightSpanRef">{{ highlightedTextComputed }}</span>
      <span>{{ afterText }}</span>
    </p>
  </div>
</template>

<script setup lang="ts">
import { computed, defineProps, defineEmits, onMounted, onBeforeUnmount, ref, watch, nextTick } from 'vue';

const props = defineProps<{
  fullText: string;
  highlightText: string; // This is the text segment to be highlighted
}>();

const containerRef = ref<HTMLElement | null>(null);
const highlightSpanRef = ref<HTMLElement | null>(null);
// Optional: if you need to reference the paragraph for complex offset calculations,
// but usually offsetTop of the span relative to the scrollable container (containerRef) is enough.
// const paragraphRef = ref<HTMLParagraphElement | null>(null);

const findStartIndex = computed(() => {
  if (!props.fullText || !props.highlightText) return -1;
  return props.fullText.indexOf(props.highlightText);
});

const beforeText = computed(() => {
  const index = findStartIndex.value;
  return index > -1 ? props.fullText.substring(0, index) : props.fullText;
});

// Renamed to avoid conflict with prop name, standard practice
const highlightedTextComputed = computed(() => {
  const index = findStartIndex.value;
  return index > -1 ? props.highlightText : '';
});

const afterText = computed(() => {
  const index = findStartIndex.value;
  if (index === -1 || !props.highlightText) {
    return index === -1 ? '' : props.fullText.substring(index + props.highlightText.length);
  }
  const startIndex = index + props.highlightText.length;
  return props.fullText.substring(startIndex);
});

const emit = defineEmits<{
  (e: 'phaseSelect', selectedText: string): void
}>();

function handleSelection() {
  const selection = window.getSelection();
  if (
    selection &&
    selection.toString().trim() &&
    containerRef.value &&
    selection.anchorNode && // Check if selection.anchorNode is not null
    containerRef.value.contains(selection.anchorNode)
  ) {
    emit('phaseSelect', selection.toString());
    // It's usually good practice to clear the selection after processing it if it's a custom interaction
    // selection.removeAllRanges(); // Uncomment if you want to clear programmatic/mouse selection after emitting
  }
}

const scrollToHighlightedText = async () => {
  // Wait for DOM updates to complete
  await nextTick();

  if (containerRef.value && highlightSpanRef.value) {
    const container = containerRef.value;
    const highlightSpan = highlightSpanRef.value;

    // If highlightText is empty, no span will be rendered, or it will have no height.
    // In this case, scroll to top or do nothing.
    if (!props.highlightText || highlightSpan.offsetHeight === 0) {
      // container.scrollTop = 0; // Optionally scroll to top if no highlight
      return;
    }

    const highlightTopOffset = highlightSpan.offsetTop; // Offset relative to the offsetParent (likely the <p>)
    // If the <p> tag has margins or the spans are nested deeper,
    // you might need a more robust way to get offset relative to 'container'.
    // For this structure, offsetTop of the span should be its distance from the top of the <p>.
    // The <p> itself is at the top of the scrollable containerRef.

    const highlightHeight = highlightSpan.offsetHeight;
    const containerHeight = container.offsetHeight;

    // Calculate desired scrollTop to center the highlight
    let targetScrollTop = highlightTopOffset - (containerHeight / 2) + (highlightHeight / 2);

    // Clamp scrollTop to be within valid bounds (0 to maxScroll)
    targetScrollTop = Math.max(0, targetScrollTop);
    // container.scrollHeight is the total height of the content within the container
    const maxScroll = container.scrollHeight - containerHeight;
    targetScrollTop = Math.min(targetScrollTop, maxScroll);
    
    // Smooth scroll for better UX
    container.scrollTo({
        top: targetScrollTop,
        behavior: 'smooth'
    });

    // Or, for immediate scroll:
    // container.scrollTop = targetScrollTop;
  }
};

onMounted(() => {
  // document.addEventListener('mouseup', handleSelection); // 'mouseup' is often better for selections than 'selectionchange' for this kind of emit
  scrollToHighlightedText();
});

onBeforeUnmount(() => {
  // document.removeEventListener('mouseup', handleSelection);
});

// Watch for changes in highlightText or fullText to re-calculate and scroll
watch(
  [() => props.highlightText, () => props.fullText],
  () => {
    scrollToHighlightedText();
  },
  { flush: 'post' } // 'post' ensures the callback runs after DOM updates
);

</script>

<style scoped>
.highlightdiv {
  padding: 1rem;
  /* IMPORTANT: Set a fixed height for the container to enable vertical centering and clipping */
  height: 100%; /* Example height, adjust as needed */
  width: 99vw; /* Consider if this should be less, e.g., 100% of its parent */
  background: url('../assets/background2.png') no-repeat center center;
  background-size: cover;
  border-radius: 8px;
  font-size: 3rem; /* Adjusted for potentially smaller container */
  color: #c0c0c0;
  overflow-y: hidden;
  scroll-behavior: smooth;
}

.highlightdiv p {
  margin: 0; /* Remove default paragraph margins if they interfere with offset calculations */
  padding: 0; /* Remove default paragraph padding */
  /* line-height: 1.4; /* Adjust line height for better readability and consistent spacing */
}

.highlight {
  color: #ffffff;
  font-weight: bold;
  background-color: rgba(0, 0, 0, 0.2); /* Optional: slight background for highlighted text */
  padding: 0.1em 0; /* Optional: slight padding if background is used */
  border-radius: 3px; /* Optional: rounded corners for background */
}

/* Optional: to make the non-highlighted text slightly less prominent */
/* .highlightdiv > p > span:not(.highlight) { */
  /* opacity: 0.7; */
/* } */
</style>