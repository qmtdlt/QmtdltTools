// Use a single AudioContext instance
let audioContext: AudioContext | null = null;
let currentSource: AudioBufferSourceNode | null = null;
let isPlaying = false; // Track if audio is currently playing

// Function to get or create the AudioContext, attempting to resume it
const getAudioContext = async (): Promise<AudioContext | null> => { // Added return type hint
    if (!audioContext) {
        try {
            audioContext = new (window.AudioContext || (window as any).webkitAudioContext)();
            console.log("AudioContext created, state:", audioContext.state);
            // Handle initial suspension after creation if needed (e.g., autoplay policy)
            if (audioContext.state === 'suspended') {
                console.log("AudioContext is suspended initially, needs user interaction to resume.");
                // Don't await resume here, let the first user-triggered play handle it.
            }
        } catch (e) {
            console.error("Failed to create AudioContext:", e);
            return null; // Return null if creation fails
        }
    }

    // Attempt to resume the context if it's suspended (e.g., due to user interaction needed)
    if (audioContext.state === 'suspended') {
        try {
            // Resume should ideally be triggered by a user gesture (e.g., button click)
            // If called outside a user gesture, it might fail.
            await audioContext.resume();
            console.log('AudioContext resumed successfully, state:', audioContext.state);
        } catch (e) {
            console.error('Failed to resume AudioContext:', e);
            // If resume fails, playback likely won't work.
            // Consider returning null or letting the decodeAudioData fail later.
            // For now, log and continue. The calling function should handle potential failures.
        }
    }
    // Check state again after attempting resume
    if (audioContext.state !== 'running') {
        console.warn(`AudioContext state is still '${audioContext.state}' after attempting resume.`);
        // Playback might still fail if not 'running'.
    }
    return audioContext;
};

export const startPlayBase64Audio = async (base64string: string, onEnded: () => void) => {
    console.log("Attempting to play audio...");

    // Stop any currently playing audio source managed by this module
    if (currentSource) {
        console.log("Stopping existing audio source before starting new one.");
        // Remove previous onended handler to prevent it firing after manual stop
        currentSource.onended = null;
        currentSource.stop();
        currentSource.disconnect(); // Disconnect to allow garbage collection
        currentSource = null;
        isPlaying = false; // Update state immediately
    }

    const context = await getAudioContext(); // Get/resume the context
    if (!context) {
        console.error("Could not get or resume AudioContext. Playback aborted.");
        onEnded(); // Signal failure or completion immediately
        return;
    }
    // Ensure context is running before proceeding
    if (context.state !== 'running') {
         console.error(`AudioContext is not running (state: ${context.state}). Playback likely blocked by browser policy. Aborting.`);
         onEnded();
         return;
    }


    try {
        const byteArray = Uint8Array.from(atob(base64string), c => c.charCodeAt(0));

        // Decode audio data and play it
        // Use Promise-based decodeAudioData for better async handling
        const audioBuffer = await context.decodeAudioData(byteArray.buffer);

        console.log("Audio data decoded successfully.");
        const source = context.createBufferSource();
        source.buffer = audioBuffer;
        source.connect(context.destination);

        // Define onended handler *before* starting
        source.onended = () => {
            console.log("Audio source playback ended naturally or was stopped.");
            isPlaying = false;
            // Clean up source reference ONLY if it's the one that ended
            // This check prevents race conditions if stopPlay was called just before onended fired
            if (currentSource === source) {
                 currentSource = null;
            }
            // Disconnect the node after it has finished playing or been stopped.
            // It's good practice even if the node is implicitly disconnected on end.
            try {
                 source.disconnect();
            } catch(e) {
                 console.warn("Error disconnecting source node:", e);
            }

            onEnded(); // Call the user-provided onEnded callback
        };

        source.start();
        currentSource = source; // Store the new source
        isPlaying = true;
        console.log("Audio source started.");

    } catch (error) {
        console.error('Error during audio playback process:', error);
        isPlaying = false;
        // Ensure currentSource is cleared if an error occurred during setup
        if (currentSource) {
             try { currentSource.disconnect(); } catch(e) {} // Attempt cleanup
             currentSource = null;
        }
        onEnded(); // Signal that this attempt failed
    }
};

export const stopPlayBase64Audio = () => {
    console.log("Attempting to stop audio.");
    if (currentSource) {
        // Remove handler before stopping to prevent onEnded logic firing from manual stop
        currentSource.onended = null;
        currentSource.stop();
        currentSource.disconnect();
        currentSource = null;
        isPlaying = false;
        console.log("Audio source stopped via stopPlayBase64Audio.");
    } else {
        console.log("No active audio source to stop.");
    }
    // Do NOT suspend the context here. Let it remain running.
    // if (audioContext && audioContext.state === 'running') {
    //      audioContext.suspend().then(() => {
    //          console.log("AudioContext suspended."); // REMOVED
    //      }).catch(e => console.error("Error suspending AudioContext:", e));
    // }
};

// Add a function to check if audio is currently playing
export const getIsPlaying = () => isPlaying;

// Add a function to clean up on component unmount
export const cleanupAudio = () => {
    console.log("Cleaning up audio resources.");
    stopPlayBase64Audio(); // Stop any playing audio first
    if (audioContext) {
        // It's generally recommended to close the context only when absolutely necessary,
        // like when the entire application is shutting down.
        // If components frequently mount/unmount, constantly closing/recreating
        // the context can be inefficient and might lead to issues.
        // Consider if closing is truly needed here or if just stopping the source is sufficient.
        // For now, we keep the close logic as it was.
        audioContext.close().then(() => {
            console.log("AudioContext closed.");
            audioContext = null; // Clear reference
        }).catch(e => console.error("Error closing AudioContext:", e));
    }
}