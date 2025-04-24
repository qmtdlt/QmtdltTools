// Use a single AudioContext instance
let audioContext: AudioContext | null = null;
let currentSource: AudioBufferSourceNode | null = null;
let isPlaying = false; // Track if audio is currently playing

// Function to get or create the AudioContext, attempting to resume it
const getAudioContext = async () => {
    if (!audioContext) {
        audioContext = new (window.AudioContext || (window as any).webkitAudioContext)();
        console.log("AudioContext created");
    }
    // Attempt to resume the context if it's suspended (common on iOS)
    if (audioContext.state === 'suspended') {
        try {
            await audioContext.resume();
            console.log('AudioContext resumed successfully');
        } catch (e) {
            console.error('Failed to resume AudioContext:', e);
            // Depending on error handling needs, might want to return null or throw
            // For now, let's just log and continue, hoping the subsequent start works.
            // The first call triggered by user gesture should handle the initial resume.
        }
    }
    return audioContext;
};

export const startPlayBase64Audio = async (base64string: string, onEnded: () => void) => {
    console.log("Attempting to play audio...");
    
    // Stop any currently playing audio source from the same context
    if (currentSource) {
        currentSource.stop();
        currentSource.disconnect(); // Disconnect to allow garbage collection
        currentSource = null;
        console.log("Stopped existing audio source.");
    }

    try {
        const context = await getAudioContext(); // Get/resume the context
        if (!context) {
            console.error("Could not get or resume AudioContext.");
            // If context is null, playback cannot proceed. Call onEnded to signal failure.
            onEnded();
            return;
        }

        const byteArray = Uint8Array.from(atob(base64string), c => c.charCodeAt(0));

        // Decode audio data and play it
        context.decodeAudioData(byteArray.buffer, (audioBuffer) => {
            console.log("Audio data decoded successfully.");
            const source = context.createBufferSource();
            source.buffer = audioBuffer;
            source.connect(context.destination);

            source.onended = () => {
                console.log("Audio source playback ended.");
                isPlaying = false;
                // Clean up source reference ONLY if it's the one that ended
                if (currentSource === source) {
                     currentSource = null;
                }
                 source.disconnect(); // Disconnect after ending
                // Do NOT close context here.
                onEnded(); // Call the user-provided onEnded callback (to fetch next chunk)
            };

            source.start();
            currentSource = source; // Store the new source
            isPlaying = true;
            console.log("Audio source started.");

        }, (error) => {
            console.error('Failed to decode audio data:', error);
            isPlaying = false;
            // Handle decoding error - maybe call onEnded?
            onEnded(); // Signal that this attempt failed
        });
    } catch (error) {
        console.error('Error obtaining, resuming, or using AudioContext:', error);
        isPlaying = false;
        // Handle errors getting/resuming context - maybe call onEnded?
        onEnded(); // Signal that this attempt failed
    }
};

export const stopPlayBase64Audio = () => {
    console.log("Attempting to stop audio.");
    if (currentSource) {
        currentSource.stop();
        currentSource.disconnect();
        currentSource = null;
        isPlaying = false;
        console.log("Audio source stopped.");
    }
    // Suspend the context when stopping, so it can be resumed later.
    if (audioContext && audioContext.state === 'running') {
         audioContext.suspend().then(() => {
             console.log("AudioContext suspended.");
         }).catch(e => console.error("Error suspending AudioContext:", e));
    }
};

// Add a function to check if audio is currently playing
export const getIsPlaying = () => isPlaying;

// Add a function to clean up on component unmount
export const cleanupAudio = () => {
    console.log("Cleaning up audio resources.");
    stopPlayBase64Audio(); // Stop any playing audio first
    if (audioContext) {
        audioContext.close().then(() => {
            console.log("AudioContext closed.");
            audioContext = null; // Clear reference
        }).catch(e => console.error("Error closing AudioContext:", e));
    }
}