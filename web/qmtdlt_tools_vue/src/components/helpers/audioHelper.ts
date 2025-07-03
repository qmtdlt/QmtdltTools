// helpers/audioHelper.ts
let audioContext: AudioContext | null = null;
let processor: ScriptProcessorNode | null = null;

export const setupAudioCapture = async (videoElement: HTMLVideoElement, pushStream: any) => {
  audioContext = new AudioContext();
  const source = audioContext.createMediaElementSource(videoElement);
  processor = audioContext.createScriptProcessor(4096, 1, 1);

  source.connect(processor);
  processor.connect(audioContext.destination);

  processor.onaudioprocess = (event) => {
    const inputData = event.inputBuffer.getChannelData(0);
    const pcmData = floatTo16BitPCM(inputData);
    pushStream.write(pcmData);
  };
};

export const stopAudioCapture = () => {
  if (processor) {
    processor.disconnect();
    processor = null;
  }
  if (audioContext) {
    audioContext.close();
    audioContext = null;
  }
};

function floatTo16BitPCM(input: Float32Array) {
  const output = new Int16Array(input.length);
  for (let i = 0; i < input.length; i++) {
    const s = Math.max(-1, Math.min(1, input[i]));
    output[i] = s < 0 ? s * 0x8000 : s * 0x7FFF;
  }
  return new Uint8Array(output.buffer);
}
