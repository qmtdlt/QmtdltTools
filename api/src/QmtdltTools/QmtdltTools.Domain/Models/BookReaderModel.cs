using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QmtdltTools.Domain.Data;
using VersOne.Epub;

namespace QmtdltTools.Domain.Models
{
    public class BookReaderModel
    {
        public List<MyPragraph> plist { get; set; }         // 电子书对象
        public ReadPosition position { get; set; }           // 位置
        public bool PositionInbook()
        {
            if(position.PragraphIndex < plist.Count && position.SentenceIndex < plist[position.PragraphIndex].Sentences.Count)
            {
                return true;
            }
            return false;
        }

        public void ResetPosition(int offsetPos)
        {
            if(offsetPos >= 0)
            {
                int cnt = 0;
                while(PositionInbook() && cnt < offsetPos - 1)      // sub 1, because cached two
                {
                    PositionNext();
                    cnt++;
                }
            }
            else
            {
                int cnt = 0;
                while (PositionInbook() && cnt <= Math.Abs(offsetPos))
                {
                    if (position.SentenceIndex > 0)
                    {
                        position.SentenceIndex--;
                    }
                    else
                    {
                        if (position.PragraphIndex > 0)
                        {
                            position.PragraphIndex--;
                            position.SentenceIndex = plist[position.PragraphIndex].Sentences.Count - 1;
                        }
                    }
                    cnt++;
                }
            }
        }


        public bool PositionNext()
        {
            if (position.SentenceIndex + 1 < plist[position.PragraphIndex].Sentences.Count)
            {
                position.SentenceIndex++;
                return true;
            }
            else
            {
                if (position.PragraphIndex + 1 < plist.Count)
                {
                    position.PragraphIndex++;
                    position.SentenceIndex = 0;
                    return true;
                }
                else
                {
                    // isEnd
                    return false;
                }
            }
        }
        public UIReadInfo GetCurrentPosInfo()
        {
            if(PositionInbook())
            {
                return new UIReadInfo() { 
                    full_pragraph_text = plist[position.PragraphIndex].PragraphText, 
                    speaking_text = plist[position.PragraphIndex].Sentences[position.SentenceIndex], 
                    speaking_buffer = new byte[0],
                    position = position 
                };
            }
            else
            {
                return null;
            }
        }
        public ConcurrentQueue<UIReadInfo> readQueue { get; set; } = new ConcurrentQueue<UIReadInfo>(); // 队列
    }

    public class UIReadInfo
    {
        public UIReadInfo()
        {
            voice_name = ApplicationConst.DefaultVoiceName;
        }
        public string voice_name { get; set; }
        public string full_pragraph_text { get; set; }
        public string speaking_text { get; set; }
        public byte[] speaking_buffer { get; set; }
        public ReadPosition position { get; set; }
    }
    public class ReadPosition
    {
        public ReadPosition()
        {
            PragraphIndex = 0;
            SentenceIndex = 0;
        }
        public int PragraphIndex { get; set; }
        public int SentenceIndex { get; set; }
    }
}
