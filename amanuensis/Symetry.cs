using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace amanuensis
{
    class Board
    {
        public char[] spaces;
        public int width;
        public int height;

        public static Board Blank(int width, int height)
        {
            var board = new Board
            {
                spaces = new char[width * height],
                width = width,
                height = height,
            };
            for (int i = 0; i < board.spaces.Length; i++) { board.spaces[i] = ' '; }
            return board;
        }

        public Board Copy()
        {
            var copy = Board.Blank(this.width, this.height);
            Array.Copy(this.spaces, copy.spaces, this.spaces.Length);
            return copy;
        }

        public void Set(int x, int y, char newValue)
        {
            this.spaces[y * this.width + x] = newValue;
        }


        public char Get(int x, int y)
        {
            return this.spaces[y * this.width + x];
        }

        public bool Empty(int x, int y)
        {
            return (y < this.height) && (x < this.width) && Char.IsWhiteSpace(Get(x, y));
        }

        public override string ToString()
        {
            StringBuilder b = new StringBuilder();
            for (int y = 0; y < this.height; y++)
            {
                for (int x = 0; x < this.width; x++)
                {
                    b.Append(Get(x, y));
                }
                b.AppendLine();
            }
            return b.ToString();
        }
    }

    class Piece
    {
        public int width;
        public int height;
        public char[] parts;

        public Piece(int width, int height, char[] parts)
        {
            this.width = width;
            this.height = height;
            this.parts = parts;
        }

        public bool IsFilled(int px, int py)
        {
            return !Char.IsWhiteSpace(Get(px, py));
        }

        public char Get(int px, int py)
        {
            return this.parts[py * this.width + px];
        }

        public override string ToString()
        {
            StringBuilder b = new StringBuilder();
            for (int y = 0; y < this.height; y++)
            {
                for (int x = 0; x < this.width; x++)
                {
                    b.Append(Get(x, y));
                }
                b.AppendLine();
            }
            return b.ToString();
        }
    }

    public static class Symetry
    {
        public static void Run()
        {
            var a_peices = new[]
            {
                new Piece(2,2,
                    new char[] {
                        ' ', 'A',
                        'A', 'A', }),
                new Piece(2,2,
                    new char[] {
                        'A', ' ',
                        'A', 'A', }),
            };

            var b_peices = new[]
            {
                new Piece(2,2,
                    new char[]
                    {
                        'B', 'B',
                        'B', ' ',
                    }),
            };

            var c_peices = new[]
            {
                new Piece(3,1,
                    new char[]
                    {
                        'C', 'C', 'C'
                    }),
            };

            var d_peices = new[]
            {
                new Piece(2, 3, new char[]
                {
                    'D', 'D',
                    'D', ' ',
                    'D', ' ',
                }),

                new Piece(2, 3, new char[]
                {
                    'D', 'D',
                    ' ', 'D',
                    ' ', 'D',
                }),

                new Piece(2, 3, new char[]
                {
                    ' ', 'D',
                    ' ', 'D',
                    'D', 'D',
                }),
            };

            var solutions =
                (from a in a_peices
                 from b in b_peices
                 from c in c_peices
                 from d in d_peices
                 from board_with_a in Emplace(a, Board.Blank(5, 5))
                 from board_with_ab in Emplace(b, board_with_a)
                 from board_with_abc in Emplace(c, board_with_ab)
                 from board_with_abcd in Emplace(d, board_with_abc)
                 where IsSymetrical(board_with_abcd)
                 where CenterLineFilled(board_with_abcd)
                 where !board_with_abcd.Empty(1, 1)
                 where !board_with_abcd.Empty(1, 3)
                 select new
                 {
                     a = a,
                     b = b,
                     c = c,
                     d = d,
                     board = board_with_abcd
                 }).ToArray();

            foreach (var solution in solutions)
            {
                Console.WriteLine("Solution: " + new string('=', 20));
                Console.WriteLine("a:\n{0}", solution.a);
                Console.WriteLine("b:\n{0}", solution.b);
                Console.WriteLine("c:\n{0}", solution.c);
                Console.WriteLine("d:\n{0}", solution.d);
                Console.WriteLine();
                Console.WriteLine("board:\n{0}", solution.board);
            }
            Console.WriteLine();
            Console.WriteLine("{0} solutions", solutions.Length);
        }

        private static bool CenterLineFilled(Board board)
        {
            for (int y = 0; y < board.height; y++)
            {
                if (board.Empty(2, y)) { return false; }
            }
            return true;
        }

        private static bool IsSymetrical(Board board)
        {
            for (int y = 0; y < board.height; y++)
            {
                for (int x = 0; x < board.width / 2; x++)
                {
                    if (board.Empty(x, y) != board.Empty(board.width - (x + 1), y))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private static IEnumerable<Board> Emplace(Piece a, Board board)
        {
            for (int y = 0; y < board.height; y++)
            {
                for (int x = 0; x < board.width; x++)
                {
                    if (Fits(a, board, x, y))
                    {
                        yield return Place(a, board, x, y);
                    }
                }
            }
        }

        private static bool Fits(Piece a, Board board, int x, int y)
        {
            for (int py = 0; py < a.height; py++)
            {
                for (int px = 0; px < a.width; px++)
                {
                    if (a.IsFilled(px, py) && !board.Empty(x + px, y + py)) { return false; }
                }
            }
            return true;
        }

        private static Board Place(Piece a, Board board, int x, int y)
        {
            var copy = board.Copy();
            for (int py = 0; py < a.height; py++)
            {
                for (int px = 0; px < a.width; px++)
                {
                    if (!Char.IsWhiteSpace(a.Get(px, py)))
                    {
                        copy.Set(x + px, y + py, a.Get(px, py));
                    }
                }
            }
            return copy;
        }
    }
}
