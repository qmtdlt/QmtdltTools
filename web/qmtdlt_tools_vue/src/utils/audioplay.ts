let currentAudio: {
    context: AudioContext;
    source: AudioBufferSourceNode;
} | null = null;

export const startPlayBase64Audio = (base64string: string, onEnded: () => void) => {
    // Stop any currently playing audio
    if (currentAudio) {
        currentAudio.source.stop();
        currentAudio.context.close();
        currentAudio = null;
    }

    // Decode Base64 string to binary data
    const byteArray = Uint8Array.from(atob(base64string), c => c.charCodeAt(0));
    const context = new AudioContext();

    // Decode audio data and play it
    context.decodeAudioData(byteArray.buffer, (audioBuffer) => {
        const source = context.createBufferSource();
        source.buffer = audioBuffer;
        source.connect(context.destination);
        source.onended = () => {
            onEnded();
            context.close();
            currentAudio = null;
        };
        source.start();
        currentAudio = { context, source };
    }, (error) => {
        console.error('Failed to decode audio data:', error);
    });
};

export const stopPlayBase64Audio = () => {
    if (currentAudio) {
        currentAudio.source.stop();
        currentAudio.context.close();
        currentAudio = null;
    }
};