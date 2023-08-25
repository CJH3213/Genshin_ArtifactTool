using Microsoft.SqlServer.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ArtifactTool
{
    internal class Guesser
    {
        // 副词条对应的每次升级随机值
        public static Dictionary<string, double[]> gSubStats = new Dictionary<string, double[]>()
        {
            {"数值攻击力", new double[] { 14,  16,  18,  19 } },
            {"数值生命值", new double[] { 209, 239, 269, 299 } },
            {"数值防御力", new double[] { 16, 19,  21,  23 } },
            {"百分比攻击力", new double[] { 4.1, 4.7, 5.3, 5.8 } },
            {"百分比生命值", new double[] { 4.1, 4.7, 5.3, 5.8 } },
            {"百分比防御力", new double[] { 5.1, 5.8, 6.6, 7.3 } },
            {"元素精通", new double[] { 16,  19,  21,  23 } },
            {"元素充能", new double[] { 4.5, 5.2, 5.8, 6.5 } },
            {"暴击率", new double[] { 2.7, 3.1, 3.5, 3.9 } },
            {"暴击伤害", new double[] { 5.4, 6.2, 7.0, 7.8 } }
        };

        private List<double[]> mGuessList = new List<double[]>();
        private string mSubStatName = "";
        private double mSubStatValue = 0;

        // 构造函数
        public Guesser(string subStatName, double finalValue) 
        {
            mSubStatName = subStatName;
            mSubStatValue = finalValue;

            GuessUpgradeOrders();
        }

        public List<double[]> GuessList
        {
            get { return mGuessList; }
        }

        // 获取副词条的所有档次数值
        public double[] GetGradeValues()
        {
            double[] subStat = null;
            bool res = gSubStats.TryGetValue(mSubStatName, out subStat);

            return res ?  subStat : new double[0];
        }

        // 根据输入的副词条和最终值生成所有可能的组合
        private void GuessUpgradeOrders()
        {
            string subStatName = mSubStatName;
            double finalValue = mSubStatValue;

            double[] subStat;
            bool res = gSubStats.TryGetValue(subStatName, out subStat);

            // 没有这个词条
            if (!res)
                return;

            // 计算大概升级了几次
            double avg = subStat.Average();
            int times = (int)(finalValue / avg + 0.5);  // 四舍五入

            // 遍历出该升级次数下的所有组合
            var list1 = GetCombinations(subStat, times - 1);
            mGuessList.AddRange(list1);
            var list2 = GetCombinations(subStat, times);
            mGuessList.AddRange(list2);
            var list3 = GetCombinations(subStat, times + 1);
            mGuessList.AddRange(list3);

            // 过滤出误差较小的组合
            ScreenApproximation(1.5);
        }

        // 基于redix进制的数加1
        private static void NextIndex(ref int[] index, int radix)
        {
            for (int i = 0; i < index.Length; i++)
            {
                index[i]++;

                if (index[i] < radix)
                    break;    // 没发生进位，直接退出

                // 发生进位，求余数，到下一位加一
                index[i] %= radix;
            }
        }

        // 根据指定词条、组合次数生成组合序列
        private static List<double[]> GetCombinations(double[] subStat, int times)
        {
            List<double[]> comb = new List<double[]>();

            if (times <= 0 || times > 6)
                return comb;

            // 遍历出该升级次数下的所有组合
            int[] index = new int[times];
            for (int i = 0; i < Math.Pow(subStat.Length, times); i++)
            {
                double[] guess = Enumerable
                    .Range(0, times)
                    .Select(ei => subStat[index[ei]])
                    .ToArray();

                comb.Add(guess);

                NextIndex(ref index, subStat.Length);
            }

            return comb;
        }

        // 数组特定方式排序
        private static void Sort(List<double[]> list, double finalValue)
        {
            list.Sort((a, b) =>
            {
                // 即使两数组内容相同但顺序不同，double.Sum的结果会有微小的精度误差
                // 这个误差量级一般为E-15
                // 特别是需要判断两个sum结果是否相等时，容易判断为不相等
                // 首先确定好可接受误差值，一般为1E-6，我这里取0.0001
                // 比较两个浮点数是否相等时，只要两数之差在0.0001内都认为两数相等
                // 若无法接受此比较方式，请将double改为decimal类型

                // 先比较两数组总和大小
                //double aSum = a.Sum();  
                //double bSum = b.Sum();
                //if (Math.Abs(aSum - bSum) > 0.0001)
                //    return aSum.CompareTo(bSum);
                double aDelta = Math.Abs(a.Sum() - finalValue);
                double bDelta = Math.Abs(b.Sum() - finalValue);
                if (Math.Abs(aDelta - bDelta) > 0.0001)
                    return aDelta.CompareTo(bDelta);

                // 若两数组总和误差相等，数组长度较短的优先
                if(a.Length != b.Length)
                    return a.Length.CompareTo(b.Length);

                // 若两数组长度相等，按数组内元素大小排序
                for (int i = Math.Min(a.Length, b.Length) - 1; i >= 0; i--)
                {
                    if (a[i] != b[i])
                        return a[i].CompareTo(b[i]); ;
                }

                // 若两数组元素相等，返回0表示
                return 0;
            });
        }

        // 筛选误差较小的组合
        private void ScreenApproximation(double maxError)
        {
            List<double[]> srcList = mGuessList;
            List<double[]> dstList = new List<double[]>();

            // 先排序
            Sort(srcList, mSubStatValue);

            // 筛选误差较小的
            foreach(var item in srcList)
            {
                double error = Math.Abs(item.Sum() - mSubStatValue);
                if(error < maxError)
                    dstList.Add(item);
            }

            mGuessList = dstList;
        }

        // 去除相同元素组合的数组
        public void ScreenSingleOrders()
        {
            List<double[]> srcList = mGuessList;
            List<double[]> dstList = new List<double[]>();

            for (int i = 0; i < srcList.Count; i++)
            {
                double[] srcItem = srcList[i];
                double[] copyItem = new double[srcItem.Length];
                srcItem.CopyTo(copyItem, 0 );
                Array.Sort(copyItem);

                // 若本元素排列和已经出现过，不添加进list
                bool existed = false;
                foreach (var item in dstList)
                    if (copyItem.SequenceEqual(item))
                    { 
                        existed = true;
                        break; 
                    }

                if(existed == false)
                    dstList.Add(copyItem);
            }

            mGuessList = dstList;
        }

        // 统计可能的升级次数
        public int[] UpgradeTimes()
        {
            HashSet<int> set = new HashSet<int>();

            foreach (var item in mGuessList)
                set.Add(item.Length);

            return set.ToArray();
        }


        public static void Printf(List<double[]> list)
        {
            //foreach (var item in guessList)
            //{
            //    foreach (var item2 in item)
            //    {
            //        Console.Write("{0}, ", item2);
            //    }
            //    Console.WriteLine(" :  {0:N2}, \n", item.Sum()-finalValue); // R格式说明符能显示浮点数误差
            //}
        }
    }
}
