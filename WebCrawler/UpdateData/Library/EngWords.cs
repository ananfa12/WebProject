using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WebCrawler
{
    public class Words : List<string>
    {
        public string Source { get; set; }
        public Words(IEnumerable<string> words) : base(words) { }
        public Trie Tree { get; set; }
        private List<KeyValuePair<int, string>> digits;
        public int BestItemCount { get; set; }

        public override string ToString()
        {
            if (this.Count == 0)
            {
                return Source;
            }
            else
            {
                var rs = "";
                foreach (var item in this)
                {
                    var ts = "";
                    var s = item.ToLower();
                    for (int i = 1; i < s.Length; i++)
                    {
                        if (s[i - 1].ToString() == " ")
                        {
                            ts += s[i].ToString().ToUpper();
                        }
                        else if (s[i].ToString() != " ")
                        {
                            ts += s[i].ToString();
                        }
                    }
                    if (digits.Count > 0)
                    {
                        for (int j = 0; j < digits.Count; j++)
                        {
                            ts = ts.Insert(digits[j].Key, digits[j].Value);
                        }
                    }
                    rs += ts + "\n";
                }
                
                return rs;
            }
            
        }

        public void Print()
        {
            if (this.Count == 0)
            {
                Console.WriteLine(Source);
            }
            else
            {
                var item = this.ToString();
                    for (int i = 0; i < item.Length; i++)
                    {
                        if (Char.IsUpper(item[i]))
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.Write(item[i].ToString().ToUpper());
                            Console.ResetColor();
                        }
                        else if (Char.IsDigit(item[i]))
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.Write(item[i].ToString());
                            Console.ResetColor();
                        }else{
                            Console.ForegroundColor = ConsoleColor.White;
                            Console.Write(item[i].ToString());
                            Console.ResetColor();
                        }
                    }
                    //Console.Write("\n");
               
            }
        }

        public string GetBest()
        {
            var itemWithRating = new Dictionary<string, double>();
            foreach (var item in this)
            {
                // calculate rating
                var filteredWords = item.Split(' ').ToArray(); //Where(i => i.Length > 2).
                var count = filteredWords.Length;
                var withS = filteredWords.Where(i => i.Length > 0 && i[i.Length - 1] == 'S').ToArray().Count();
                var shortW = filteredWords.Where(i => i.Length < 3).ToArray().Count();
                itemWithRating[item] = 1000 - count * 100 - 10 * shortW - withS;
                
                //var cri = item.Split(' ').Length;
                //if (cri < cr)
                //{
                //    cr = cri;
                //    rs = item; // tbd
                //}
            }
            if (itemWithRating.Count > 0)
            {
                var max = itemWithRating.Max(i => i.Value);
                var best = itemWithRating.Where(i => i.Value == max).FirstOrDefault().Key;
                if (best.Length > 0)
                {
                    BestItemCount = best.Trim().Split(' ').Length;
                    return PrepareItem(best);
                }else{
                    BestItemCount = 0;
                    return Source;
                }
                
            }
            else
            {
                return Source;
            }
        }

        public string FindBestIncSeparators()
        {
            string[] res = Regex.Split(Source, "[0-9,-]");
            var s = Source;
            if (res.Length == 0)
            {
                return Source;
            }

            try
            {
                foreach (var item in res)
                {
                    if (string.IsNullOrEmpty(item)) continue;
                    this.FindAndSplit((string)item);
                    var b = this.GetBest();
                    s = s.Replace(item, b);
                }
                return s;
            }
            catch (Exception ex)
            {

                return Source;
            }
            
        }

        public string SplitKeywords(string s)
        {
            var res = s;
            var ar = new List<char>();
            for (int i = 0; i < res.Length; i++)
            {
                if (Char.IsUpper(res[i]))
                {
                    ar.Add(res[i]);
                }
            }
            
            foreach (var c in ar)
            {
                var cs = c.ToString();
                res = res.Replace(cs, " " + cs.ToLower());
            }

            res = Regex.Replace(res, "[0-9,-]", " ");
            return res.Replace("  ", " ").ToLower().Trim();
        }


        private string PrepareItem(string item)
        {
            var ts = "";
            var s = " " + item.ToLower();
            for (int i = 1; i < s.Length; i++)
            {
                if (s[i - 1].ToString() == " ")
                {
                    ts += s[i].ToString().ToUpper();
                }
                else if (s[i].ToString() != " ")
                {
                    ts += s[i].ToString();
                }
            }
            return ts.Replace(" ", "");
        }

        public void FindAndSplit(string s)
        {
            Source = s;
            this.Clear();
            SearchForWords(s);
        }


        private bool isWord(Trie.Node node, string str)
        {
            const string Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            //var n = 0;

            var curNode = node;
            for (int i = 0; i < str.Length; i++)
            {
                var l = new Letter();
                l.Index = Chars.IndexOf(str[i]);
                if (curNode.Edges.ContainsKey(l))
                {
                    curNode = curNode.Edges[l];
                }
                else
                {
                    return false;
                }
            }
            return curNode.IsTerminal;
        }


        private void SearchForWords(Trie.Node node, string prev, string cur, string rem)
        {
            if (string.IsNullOrEmpty(cur)) return;
            if (isWord(node, cur))
            {
                if (cur.Length == rem.Length)
                {
                    this.Add(prev + " " + cur);
                }
                SearchForWords(node, prev + " " + cur, rem.Substring(cur.Length), rem.Substring(cur.Length));
            }
            SearchForWords(node, prev, cur.Substring(0, cur.Length - 1), rem);
        }

        private void SearchForWords(string str)
        {
            var s = Prepare(str); 
            SearchForWords(Tree.Root, "", s, s);
        }

        private string Prepare(string str)
        {
            digits = new List<KeyValuePair<int, string>>();
            var res = "";
            for (int i = 0; i < str.Length; i++)
            {
                if (Char.IsDigit(str[i]))
                {
                    digits.Add(new KeyValuePair<int,string>(i, str[i].ToString()));
                }
                else if (Char.IsLetter(str[i]))
                {
                    res += str[i].ToString();
                }
            }
            return res.ToUpper();
        }
    }


    

    public class Trie
    {
        public class Node
        {
            public string Word;
            public bool IsTerminal { get { return Word != null; } }
            public Dictionary<Letter, Node> Edges = new Dictionary<Letter, Node>();
        }

        public Node Root = new Node();

        public Trie(string[] words)
        {
            for (int w = 0; w < words.Length; w++)
            {
                var word = words[w];
                var node = Root;
                for (int len = 1; len <= word.Length; len++)
                {
                    var letter = word[len - 1];
                    Node next;
                    if (!node.Edges.TryGetValue(letter, out next))
                    {
                        next = new Node();
                        if (len == word.Length)
                        {
                            next.Word = word;
                        }
                        node.Edges.Add(letter, next);
                    }
                    node = next;
                }
            }
        }
    }

    public struct Letter
    {
        public const string Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        public static implicit operator Letter(char c)
        {
            return new Letter() { Index = Chars.IndexOf(c) };
        }
        public int Index;
        public char ToChar()
        {
            return Chars[Index];
        }
        public override string ToString()
        {
            return Chars[Index].ToString();
        }
    }

}
