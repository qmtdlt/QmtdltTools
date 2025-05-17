using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
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
            CalcPosProg();
            if(position.PragraphIndex < plist.Count && position.SentenceIndex < plist[position.PragraphIndex].Sentences.Count 
                && (position.ProgressValue >= 0 && position.ProgressValue <= 100))
            {
                return true;
            }
            return false;
        }
        public void ResetProgress(int progress)
        {
            if(progress < 0 || progress > 100)
            {
                return;
            }
            else
            {
                position.ProgressValue = progress;
                position.PragraphIndex = (int)((double)progress / 100d * (double)plist.Count);
                position.SentenceIndex = 0;
            }
        }
        
        public bool PositionNext()
        {
            if (position.SentenceIndex + 1 < plist[position.PragraphIndex].Sentences.Count)
            {
                position.SentenceIndex++;
                CalcPosProg();
                return true;
            }
            else
            {
                if (position.PragraphIndex + 1 < plist.Count)
                {
                    position.PragraphIndex++;
                    position.SentenceIndex = 0;
                    CalcPosProg();
                    return true;
                }
                else
                {
                    // isEnd
                    return false;
                }
            }
        }
        void CalcPosProg()
        {
            position.ProgressValue = ((double)(plist.Take(position.PragraphIndex).Sum(t => t.Sentences.Count) + position.SentenceIndex) / (double)plist.Sum(t=>t.Sentences.Count) * 100d);
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
                // 位置越界
                position = new ReadPosition() { PragraphIndex = 0, SentenceIndex = 0};
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
        public double ProgressValue { get; set; }
    }
}
