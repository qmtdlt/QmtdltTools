
import request from '@/utils/request' // Import your request utility
/*
    region:eastus
    voice_name:zh-CN-YunxiNeural
*/
const speakText = async (region:string,voice_name:string,content_text:string): Promise<void> => {    
    let audioSrc = '';
    // 构造 SSML
    const ssml: string = `<speak version="1.0" xmlns="http://www.w3.org/2001/10/synthesis" xml:lang="zh-CN">
                        <voice name="${voice_name}">
                          ${content_text}
                          </voice>
                      </speak>`;
  
    try {
      // 调用微软 TTS API
      const response = await request.post(
        `https://${region}.tts.speech.microsoft.com/cognitiveservices/v1`,
        ssml,
        {
          headers: {
            'Ocp-Apim-Subscription-Key': '', // 替换为你的密钥
            'Content-Type': 'application/ssml+xml',
            'X-Microsoft-OutputFormat': 'riff-24khz-16bit-mono-pcm',
          },
          responseType: 'blob', // 接收二进制音频流
        }
      );
  
      // 将音频数据转换为 Blob 并生成 URL
      const audioBlob = new Blob([response], { type: 'audio/wav' });
      audioSrc = URL.createObjectURL(audioBlob);
      debugger;
    } catch (err) {
      console.error('Error fetching audio:', err);      
    } finally {
        // 释放 URL 对象
        if (audioSrc) {
            URL.revokeObjectURL(audioSrc);
        }
    }
  };