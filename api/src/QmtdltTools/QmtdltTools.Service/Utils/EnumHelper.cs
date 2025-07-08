using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace QmtdltTools.Service.Utils
{
    public static class EnumHelper
    {
        public static List<ComboxSelectItem<T>> GetComboxList<T>() where T : Enum
        {
            var type = typeof(T);
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static);

            var res = fields.Select(field =>
            {
                // 获取 DescriptionAttribute
                var descriptionAttr = field.GetCustomAttribute<DescriptionAttribute>();
                string description = descriptionAttr != null ? descriptionAttr.Description : field.Name;

                // 获取枚举值
                var enumValue = (T)field.GetValue(null);

                return new
                {
                    Item = new ComboxSelectItem<T>
                    {
                        id = enumValue,
                        text = description
                    }
                };
            })
            .Select(x => x.Item)
            .ToList();

            return res;
        }
        public static List<string> GetDescriptionList<T>()
        {
            List<string> res = new List<string>();
            var type = typeof(T);

            MemberInfo[] members = type.GetMembers();

            if (members != null && members.Length > 0)
            {
                foreach (var memberInfo in members)
                {
                    // 获取Description属性
                    var attrs = memberInfo.GetCustomAttributes(typeof(DescriptionAttribute), false);
                    if (attrs != null && attrs.Length > 0)
                    {
                        res.Add(((DescriptionAttribute)attrs[0]).Description);
                    }
                }
            }
            return res;
        }
        public static string GetStatusText<T>(byte input, T statusEnum, string otherShowText) where T : Enum
        {
            byte enumValue = Convert.ToByte(statusEnum);
            int enumIntValue = Convert.ToInt32(statusEnum); // 将枚举转换为整数
            byte enumByteValue = (byte)enumIntValue; // 将整数转换为字节

            if ((input & enumByteValue) == enumByteValue)
            {
                return statusEnum.GetDescription();
            }
            return otherShowText;
        }
        public static string GetEumText<T>(byte input, T[] eums, string otherShowText) where T : Enum
        {
            eums = eums.OrderByDescending(x => x).ToArray();        // 优先取最大值的描述
            foreach (var statusEnum in eums)
            {
                byte eum = Convert.ToByte(statusEnum);
                if (((T)(object)(Convert.ToByte(input & eum))).Equals(statusEnum))
                {
                    return statusEnum.GetDescription();
                }
            }
            return otherShowText;
        }
        public static string GetDescription<T>(this T input) where T : Enum
        {
            // 获取并返回枚举值的Description信息
            // 获取枚举值类型
            Type type = input.GetType();

            // 获取枚举值成员信息
            MemberInfo[] memberInfo = type.GetMember(input.ToString());

            if (memberInfo != null && memberInfo.Length > 0)
            {
                // 获取Description属性
                var attrs = memberInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);
                if (attrs != null && attrs.Length > 0)
                {
                    return ((DescriptionAttribute)attrs[0]).Description;
                }
            }

            // 如果没有Description属性，则返回枚举值的名称
            return input.ToString();
        }
    }
}
