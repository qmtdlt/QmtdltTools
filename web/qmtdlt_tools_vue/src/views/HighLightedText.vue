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
  height: calc(100% - 20px); /* Example height, adjust as needed */
  border-radius: 8px;
  width: calc(100% - 20px); /* Consider if this should be less, e.g., 100% of its parent */
  background: url('../assets/background2.png') no-repeat center center;
  background-size: cover;
  font-size: 2rem; /* Adjusted for potentially smaller container */
  color: #c0c0c0;
  overflow-y: auto;
  scroll-behavior: smooth;
  margin: 10px 10px 0px 10px;
}

/* 美化滚动条样式 */
.highlightdiv::-webkit-scrollbar {
  width: 10px;
  background: transparent;
}
.highlightdiv::-webkit-scrollbar-thumb {
  background: rgba(64, 158, 255, 0.25);
  border-radius: 8px;
  border: 2px solid #f7fafd;
  min-height: 40px;
}
.highlightdiv::-webkit-scrollbar-thumb:hover {
  background: rgba(64, 158, 255, 0.45);
}
.highlightdiv::-webkit-scrollbar-track {
  background: transparent;
}

/* Firefox */
.highlightdiv {
  scrollbar-width: thin;
  scrollbar-color: #a0cfff #f7fafd;
  scrollbar-gutter: stable both-edges;
  /* Firefox 允许 thumb 圆角通过 track/background 颜色对比实现 */
}

.highlightdiv p {
  margin: 0; /* Remove default paragraph margins if they interfere with offset calculations */
  padding: 0; /* Remove default paragraph padding */
  font-family: 'Arial', sans-serif; /* Use a standard font for better cross-browser consistency */
  
  /* line-height: 1.4; /* Adjust line height for better readability and consistent spacing */
}

.highlight {
  color: #ffffff;
  font-weight: bold;
  background-color: rgba(0, 0, 0, 0.2); /* Optional: slight background for highlighted text */
  padding: 0.2em 0.4em 0.4em 0.4em; /* Optional: slight padding if background is used */
}

</style>