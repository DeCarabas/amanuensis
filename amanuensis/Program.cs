using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace amanuensis
{
    enum Direction { None, N, S, E, W }
    enum Marker { None, Start, Dot, TP, End }

    class Junction
    {
        public int id = -1;
        public Tuple<int, int> position;
        public Direction exit = Direction.None;
        public int N = -1;
        public int S = -1;
        public int E = -1;
        public int W = -1;
        public Marker marker = Marker.None;

        public Junction Clone()
        {
            return new Junction
            {
                id = this.id,
                position = this.position,
                exit = this.exit,
                N = this.N,
                S = this.S,
                E = this.E,
                W = this.W,
                marker = this.marker,
            };
        }

        public int GetNeighbor(Direction direction)
        {
            switch (direction)
            {
                case Direction.N: return this.N;
                case Direction.S: return this.S;
                case Direction.E: return this.E;
                case Direction.W: return this.W;
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction));
            }
        }
    }

    static class ArrayExtensions
    {
        public static T[] Clone<T>(this T[] array)
        {
            var clone = new T[array.Length];
            Array.Copy(array, clone, array.Length);
            return clone;
        }
    }


    class Panel
    {
        public Junction[] junctions;

        public Panel Clone()
        {
            var clone = new Panel { junctions = new Junction[this.junctions.Length] };
            for (int i = 0; i < this.junctions.Length; i++)
            {
                clone.junctions[i] = this.junctions[i].Clone();
            }
            return clone;
        }
    }

    struct Walk
    {
        public Panel panel;
        public int start;
    }


    static class EaterMaze
    {
        public static void Run()
        {
            var maze = @"
      E
      |
    1-X-X-X-X-X-X-1
        | | | | |
      X-X-X-X-X-X
      | | | | |
    X-X-X-X-X-X
    | | | | |
  X-X-X-X-X-X
  | | | | |
2-O-X-X-X-O-O-2
";


            Panel panel = ParseMaze(maze);

            var solutions =
                (from start in Enumerable.Range(0, panel.junctions.Length)
                 where panel.junctions[start].marker == Marker.Start
                 from walk in TracePanel(panel, start)
                 where AllDotsCovered(walk)
                 select walk).ToArray();

            Console.WriteLine("{0} solutions", solutions.Length);
            Console.WriteLine();
            
            foreach (Walk solution in solutions)
            {
                Console.WriteLine("Solution: {0}", new String('=', 20));
                int pos = solution.start;
                while(true)
                {
                    Junction j = solution.panel.junctions[pos];
                    Console.WriteLine("{0}, {1}, {2}", j.position, j.marker, j.exit);
                    if (j.exit == Direction.None)
                    {
                        if (j.marker != Marker.End) { throw new InvalidOperationException("Bad path!"); }
                        break;
                    }
                    pos = j.GetNeighbor(j.exit);
                }
                Console.WriteLine();
            }            
        }

        static Panel ParseMaze(string maze)
        {
            string[] lines = maze.Split('\n');
            int height = lines.Length;
            int width = lines.Select(l => l.Length).Max();

            // First pass, extract nodes.
            List<Junction> junctionsById = new List<Junction>();
            Dictionary<Tuple<int, int>, Junction> junctions = new Dictionary<Tuple<int, int>, Junction>();
            Dictionary<char, List<Tuple<int, int>>> connectorPosition = new Dictionary<char, List<Tuple<int, int>>>();
            for (int y = 0; y < height; y++)
            {
                string line = lines[y];
                for (int x = 0; x < line.Length; x++)
                {
                    char c = line[x];
                    if (Char.IsWhiteSpace(c)) { continue; }
                    if (IsEdge(c)) { continue; }

                    var pos = Tuple.Create(x, y);

                    var j = new Junction();
                    j.id = junctionsById.Count;
                    j.position = pos;
                    junctionsById.Add(j);

                    switch (c)
                    {
                        case 'X': j.marker = Marker.Dot; break;
                        case 'E': j.marker = Marker.End; break;
                        case 'O': j.marker = Marker.Start; break;
                    }
                    if (Char.IsDigit(c))
                    {
                        j.marker = Marker.TP;
                        List<Tuple<int, int>> posList;
                        if (!connectorPosition.TryGetValue(c, out posList))
                        {
                            posList = new List<Tuple<int, int>>();
                            connectorPosition.Add(c, posList);
                        }
                        posList.Add(pos);
                    }
                    junctions[pos] = j;
                }
            }

            // Now connect with edges!
            for (int y = 0; y < height; y++)
            {
                string line = lines[y];
                for (int x = 0; x < line.Length; x++)
                {
                    char c = line[x];
                    if (!IsEdge(c)) { continue; }
                    if (c == '|')
                    {
                        Tuple<int, int> n = Tuple.Create(x, y - 1);
                        Tuple<int, int> s = Tuple.Create(x, y + 1);

                        junctions[n].S = junctions[s].id;
                        junctions[s].N = junctions[n].id;
                    }
                    else if (c == '-')
                    {
                        Tuple<int, int> w = Tuple.Create(x - 1, y);
                        Tuple<int, int> e = Tuple.Create(x + 1, y);

                        junctions[w].E = junctions[e].id;
                        junctions[e].W = junctions[w].id;
                    }
                    else
                    {
                        throw new InvalidOperationException("Update list of edges fool");
                    }
                }
            }

            // Now stitch together teleporters. Remember, these things stitch out of existence, so we stich W of TP to
            // E of other TP, &c.
            foreach (List<Tuple<int, int>> posList in connectorPosition.Values)
            {
                int n = -1, s = -1, e = -1, w = -1;
                foreach (Tuple<int, int> pos in posList)
                {
                    Junction j = junctions[pos];
                    if (j.N >= 0)
                    {
                        if (n >= 0)
                        {
                            throw new InvalidOperationException(String.Format("Double N edge for pos {0}", pos));
                        }
                        n = j.N;
                    }
                    if (j.S >= 0)
                    {
                        if (s >= 0)
                        {
                            throw new InvalidOperationException(String.Format("Double S edge for pos {0}", pos));
                        }
                        s = j.S;
                    }
                    if (j.E >= 0)
                    {
                        if (e >= 0)
                        {
                            throw new InvalidOperationException(String.Format("Double E edge for pos {0}", pos));
                        }
                        e = j.E;
                    }
                    if (j.W >= 0)
                    {
                        if (w >= 0)
                        {
                            throw new InvalidOperationException(String.Format("Double W edge for pos {0}", pos));
                        }
                        w = j.W;
                    }
                }
                foreach (Tuple<int, int> pos in posList)
                {
                    Junction j = junctions[pos];
                    if (j.W >= 0 && e >= 0) { junctionsById[j.W].E = e; }
                    if (j.E >= 0 && w >= 0) { junctionsById[j.E].W = w; }
                    if (j.N >= 0 && s >= 0) { junctionsById[j.N].S = s; }
                    if (j.S >= 0 && n >= 0) { junctionsById[j.S].N = n; }
                }
            }

            return new Panel
            {
                junctions = junctionsById.ToArray(),
            };
        }

        static bool IsEdge(char c) { return c == '|' || c == '-'; }

        static bool AllDotsCovered(Walk walk)
        {
            return walk.panel.junctions.All(j => j.marker != Marker.Dot || j.exit != Direction.None);
        }

        static IEnumerable<Walk> TracePanel(Panel panel, int start)
        {
            return TracePanel(panel, start, start);
        }

        static IEnumerable<Walk> TracePanel(Panel panel, int start, int position)
        {
            if (position < 0) { yield break; }
            Junction j = panel.junctions[position];
            if (j.exit != Direction.None) { yield break; } // Already visited.
            if (j.marker == Marker.End)
            {
                var winner = panel.Clone();
                yield return new Walk { panel = winner, start = start };
            }

            j.exit = Direction.N;
            foreach (Walk w in TracePanel(panel, start, j.N)) { yield return w; }
            j.exit = Direction.S;
            foreach (Walk w in TracePanel(panel, start, j.S)) { yield return w; }
            j.exit = Direction.E;
            foreach (Walk w in TracePanel(panel, start, j.E)) { yield return w; }
            j.exit = Direction.W;
            foreach (Walk w in TracePanel(panel, start, j.W)) { yield return w; }
            j.exit = Direction.None;
        }
    }

    class Program
    {
        static void Main()
        {
            EaterMaze.Run();
        }
    }
}
