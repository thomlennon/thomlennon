using System;
using System.Collections.Generic;
using System.IO;

using System.Linq;

using System.Text;

using Microsoft.Z3;


using Microsoft.Spark.Sql;


namespace ESGF.Sudoku.Spark.RecursiveSearch
{




    public class Sudoku
    {


        public static readonly int[] Indices = Enumerable.Range(0, 9).ToArray();



        /// <summary>
        /// constructor that initializes the board with 81 cells
        /// </summary>
        /// <param name="cells"></param>
        public Sudoku(IEnumerable<int> cells)
        {
            var enumerable = cells.ToList();
            if (enumerable.Count != 81)
            {
                throw new ArgumentException("Sudoku should have exactly 81 cells", nameof(cells));
            }
            Cells = new List<int>(enumerable);
        }

        public Sudoku()
        {
        }



        // The List property makes it easier to manipulate cells,
        public List<int> Cells { get; set; } = Enumerable.Repeat(0, 81).ToList(); /*on garde */

        /// <summary>
        /// Easy access by row and column number
        /// </summary>
        /// <param name="x">row number (between 0 and 8)</param>
        /// <param name="y">column number (between 0 and 8)</param>
        /// <returns>value of the cell</returns>
        public int GetCell(int x, int y) /*on garde */
        {
            return Cells[(9 * x) + y];
        }

        /// <summary>
        /// Easy setter by row and column number
        /// </summary>
        /// <param name="x">row number (between 0 and 8)</param>
        /// <param name="y">column number (between 0 and 8)</param>
        /// <param name="value">value of the cell to set</param>
        public void SetCell(int x, int y, int value)
        {
            Cells[(9 * x) + y] = value;
        }

        /// <summary>
        /// Displays a Sudoku in an easy-to-read format
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var lineSep = new string('-', 31);
            var blankSep = new string(' ', 8);

            var output = new StringBuilder();
            output.Append(lineSep);
            output.AppendLine();

            for (int row = 1; row <= 9; row++)
            {
                output.Append("| ");
                for (int column = 1; column <= 9; column++)
                {

                    var value = Cells[(row - 1) * 9 + (column - 1)];

                    output.Append(value);
                    if (column % 3 == 0)
                    {
                        output.Append(" | ");
                    }
                    else
                    {
                        output.Append("  ");
                    }
                }

                output.AppendLine();
                if (row % 3 == 0)
                {
                    output.Append(lineSep);
                }
                else
                {
                    output.Append("| ");
                    for (int i = 0; i < 3; i++)
                    {
                        output.Append(blankSep);
                        output.Append("| ");
                    }
                }
                output.AppendLine();
            }

            return output.ToString();
        }

        /// <summary>
        /// Displays a Sudoku in an easy-to-read format
        /// </summary>
        /// <returns></returns>
        public string ToStringGenetic()
        {
            var output = new StringBuilder();

            for (int row = 1; row <= 9; row++)
            {
                for (int column = 1; column <= 9; column++)
                {

                    var value = Cells[(row - 1) * 9 + (column - 1)];

                    output.Append(value);
                }
            }
            return output.ToString();
        }


        public int[] GetPossibilities(int x, int y)
        {
            if (x < 0 || x >= 9 || y < 0 || y >= 9)
            {
                throw new ApplicationException("Invalid Coodrindates");
            }

            bool[] used = new bool[9];
            for (int i = 0; i < 9; i++)
            {
                used[i] = false;
            }

            for (int ix = 0; ix < 9; ix++)
            {
                if (GetCell(ix, y) != 0)
                {
                    used[GetCell(ix, y) - 1] = true;
                }
            }

            for (int iy = 0; iy < 9; iy++)
            {
                if (GetCell(x, iy) != 0)
                {
                    used[GetCell(x, iy) - 1] = true;
                }
            }

            int sx = (x / 3) * 3;
            int sy = (y / 3) * 3;

            for (int ix = 0; ix < 3; ix++)
            {
                for (int iy = 0; iy < 3; iy++)
                {
                    if (GetCell(sx + ix, sy + iy) != 0)
                    {
                        used[GetCell(sx + ix, sy + iy) - 1] = true;
                    }
                }
            }

            List<int> res = new List<int>();

            for (int i = 0; i < 9; i++)
            {
                if (used[i] == false)
                {
                    res.Add(i + 1);
                }
            }

            return res.ToArray();
        }



        /// <summary>
        /// Parses a single Sudoku
        /// </summary>
        /// <param name="sudokuAsString">the string representing the sudoku</param>
        /// <returns>the parsed sudoku</returns>

        public static Sudoku Parse(string sudokuAsString)
        {
            return ParseMulti(new[] { sudokuAsString })[0];
        }

        /// <summary>
        /// Parses a file with one or several sudokus
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns>the list of parsed Sudokus</returns>
        public static List<Sudoku> ParseFile(string fileName) /*on garde */
        {
            return ParseMulti(File.ReadAllLines(fileName));
        }

        /// <summary>
        /// Parses a list of lines into a list of sudoku, accounting for most cases usually encountered
        /// </summary>
        /// <param name="lines">the lines of string to parse</param>
        /// <returns>the list of parsed Sudokus</returns>
        public static List<Sudoku> ParseMulti(string[] lines) /*on garde */
        {
            var toReturn = new List<Sudoku>();
            var cells = new List<int>(81);
            // we ignore lines not starting with a sudoku character
            foreach (var line in lines.Where(l => l.Length > 0
                                                 && IsSudokuChar(l[0])))
            {
                foreach (char c in line)
                {
                    //we ignore lines not starting with cell chars
                    if (IsSudokuChar(c))
                    {
                        if (char.IsDigit(c))
                        {
                            // if char is a digit, we add it to a cell
                            cells.Add((int)Char.GetNumericValue(c));
                        }
                        else
                        {
                            // if char represents an empty cell, we add 0
                            cells.Add(0);
                        }
                    }
                    // when 81 cells are entered, we create a sudoku and start collecting cells again.
                    if (cells.Count == 81)
                    {
                        toReturn.Add(new Sudoku() { Cells = new List<int>(cells) });
                        // we empty the current cell collector to start building a new Sudoku
                        cells.Clear();
                    }

                }
            }

            return toReturn;
        }


        /// <summary>
        /// identifies characters to be parsed into sudoku cells
        /// </summary>
        /// <param name="c">a character to test</param>
        /// <returns>true if the character is a cell's char</returns>
        private static bool IsSudokuChar(char c) /*on garde */
        {
            return char.IsDigit(c) || c == '.' || c == 'X' || c == '-';
        }

        


        


        public int NbErrors(Sudoku originalPuzzle)
        {
            // We use a large lambda expression to count duplicates in rows, columns and boxes
            var cellsToTest = this.Cells.Select((c, i) => new { index = i, cell = c }).ToList();
            var toTest = cellsToTest.GroupBy(x => x.index / 9).Select(g => g.Select(c => c.cell)) // rows
                .Concat(cellsToTest.GroupBy(x => x.index % 9).Select(g => g.Select(c => c.cell))) //columns
                .Concat(cellsToTest.GroupBy(x => x.index / 27 * 27 + x.index % 9 / 3 * 3).Select(g => g.Select(c => c.cell))); //boxes
            var toReturn = toTest.Sum(test => test.GroupBy(x => x).Select(g => g.Count() - 1).Sum()); // Summing over duplicates
            toReturn += cellsToTest.Count(x => originalPuzzle.Cells[x.index] > 0 && originalPuzzle.Cells[x.index] != x.cell); // Mask
            return toReturn;
        }

        public bool IsValid(Sudoku originalPuzzle)
        {
            return NbErrors(originalPuzzle) == 0;
        }

    }
 /*//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////*/  
   




    /*///////////////////////////////////////////////////////////////////////////////////////////////////////////////*/

    public class Z3Solver 
    {
        protected static Context z3Context = new Context();

        // 9x9 matrix of integer variables
        static IntExpr[][] X = new IntExpr[9][];
        static BoolExpr _GenericContraints;

        private static Solver _reusableZ3Solver;

        static Z3Solver()
        {
            PrepareVariables();
        }

        public static BoolExpr GenericContraints /* ON GARDE */
        {
            get
            {
                if (_GenericContraints == null)
                {
                    _GenericContraints = GetGenericConstraints();
                }
                return _GenericContraints;
            }
        }

        public static Solver ReusableZ3Solver
        {
            get
            {
                if (_reusableZ3Solver == null)
                {
                    _reusableZ3Solver = z3Context.MkSolver();
                    _reusableZ3Solver.Assert(GenericContraints);
                }
                return _reusableZ3Solver;
            }
        }



        BoolExpr GetPuzzleConstraint(Sudoku instance)
        {
            BoolExpr instance_c = z3Context.MkTrue();
            for (int i = 0; i < 9; i++)
                for (int j = 0; j < 9; j++)
                    if (instance.GetCell(i, j) != 0)
                    {
                        instance_c = z3Context.MkAnd(instance_c,
                            (BoolExpr)
                            z3Context.MkEq(X[i][j], z3Context.MkInt(instance.GetCell(i, j))));
                    }
            return instance_c;
        }

        static void PrepareVariables()
        {
            for (uint i = 0; i < 9; i++)
            {
                X[i] = new IntExpr[9];
                for (uint j = 0; j < 9; j++)
                    X[i][j] = (IntExpr)z3Context.MkConst(z3Context.MkSymbol("x_" + (i + 1) + "_" + (j + 1)), z3Context.IntSort);
            }
        }

        static BoolExpr GetGenericConstraints() /*on garde */
        {



            // each cell contains a value in {1, ..., 9}
            BoolExpr[][] cells_c = new BoolExpr[9][];
            for (uint i = 0; i < 9; i++)
            {
                cells_c[i] = new BoolExpr[9];
                for (uint j = 0; j < 9; j++)
                    cells_c[i][j] = z3Context.MkAnd(z3Context.MkLe(z3Context.MkInt(1), X[i][j]),
                        z3Context.MkLe(X[i][j], z3Context.MkInt(9)));
            }

            // each row contains a digit at most once
            BoolExpr[] rows_c = new BoolExpr[9];
            for (uint i = 0; i < 9; i++)
                rows_c[i] = z3Context.MkDistinct(X[i]);


            // each column contains a digit at most once
            BoolExpr[] cols_c = new BoolExpr[9];
            for (uint j = 0; j < 9; j++)
            {
                Expr[] column = new Expr[9];
                for (uint i = 0; i < 9; i++)
                    column[i] = X[i][j];

                cols_c[j] = z3Context.MkDistinct(column);
            }

            // each 3x3 square contains a digit at most once
            BoolExpr[][] sq_c = new BoolExpr[3][];
            for (uint i0 = 0; i0 < 3; i0++)
            {
                sq_c[i0] = new BoolExpr[3];
                for (uint j0 = 0; j0 < 3; j0++)
                {
                    Expr[] square = new Expr[9];
                    for (uint i = 0; i < 3; i++)
                        for (uint j = 0; j < 3; j++)
                            square[3 * i + j] = X[3 * i0 + i][3 * j0 + j];
                    sq_c[i0][j0] = z3Context.MkDistinct(square);
                }
            }

            var toReturn = z3Context.MkTrue();
            foreach (BoolExpr[] t in cells_c)
                toReturn = z3Context.MkAnd(z3Context.MkAnd(t), toReturn);
            toReturn = z3Context.MkAnd(z3Context.MkAnd(rows_c), toReturn);
            toReturn = z3Context.MkAnd(z3Context.MkAnd(cols_c), toReturn);
            foreach (BoolExpr[] t in sq_c)
                toReturn = z3Context.MkAnd(z3Context.MkAnd(t), toReturn);
            return toReturn;
        }

        protected Sudoku SolveWithSubstitutions(String sudoku) /*on garde */
        {



            
            var instance = Sudoku.Parse(sudoku);

            var substExprs = new List<Expr>();
            var substVals = new List<Expr>();

            for (int i = 0; i < 9; i++)
                for (int j = 0; j < 9; j++)
                    if (instance.GetCell(i, j) != 0)
                    {
                        substExprs.Add(X[i][j]);
                        substVals.Add(z3Context.MkInt(instance.GetCell(i, j)));
                    }
            BoolExpr instance_c = (BoolExpr)GenericContraints.Substitute(substExprs.ToArray(), substVals.ToArray());

            var z3Solver = GetSolver();
            z3Solver.Assert(instance_c);

            if (z3Solver.Check() == Status.SATISFIABLE)
            {
                Model m = z3Solver.Model;
                for (int i = 0; i < 9; i++)
                    for (int j = 0; j < 9; j++)
                    {
                        if (instance.GetCell(i, j) == 0)
                        {
                            instance.SetCell(i, j, ((IntNum)m.Evaluate(X[i][j])).Int);
                        }
                    }
            }
            else
            {
                Console.WriteLine("Failed to solve sudoku");
            }
            return instance;
        }      

        protected virtual Solver GetSolver() /*on garde */
        {
            return z3Context.MkSolver();
        }


        public virtual Sudoku  Solve(string Sudoku ) /*on garde*/
        {
            return SolveWithSubstitutions(Sudoku);
        }
    }


    


    /*//////////////////////////////////////////////////////////////////////////////////////////////////////////////////*/
    
    /*////////////////////////////////////////////////////////////////////////////////////////////////////////////////*/


    
    /*///////////////////////////////////////////////////////////////////////////////////////////////////////////////*/

    class Program
    {


        public static String Solution(string sudoku)
        {
            var z3Context = new Z3Solver();
            return z3Context.Solve(sudoku).ToString();
        }

        private static void Sudokures(int nrows, String _filePath)
        {

            //var sudokus = SudokuHelper.GetSudokus(_filePath);

            //Console.WriteLine($"Choose a puzzle index between 1 and {sudokus.Count}");
            //var strIdx = Console.ReadLine();
            //int intIdx;
            //int.TryParse(strIdx, out intIdx);
            //var targetSudoku = sudokus[intIdx - 1];

            Console.WriteLine("Chosen Puzzle:");
            //Console.WriteLine(targetSudoku.ToString());

            // Initialisation de la session Spark
            SparkSession spark = SparkSession
            .Builder()
            .Config("spark.executor.memory", "4G")
            .GetOrCreate();
            //.AppName("Resolution of " + nrows + " sudokus using DlxLib with " + cores + " cores and " + nodes + " instances")
            //.Config("spark.driver.cores", cores)
            //.Config("spark.executor.instances", nodes)
            //.Config("spark.executor.memory", mem)
            //.GetOrCreate();

            // Intégration du csv dans un dataframe
            DataFrame df = spark
                .Read()
                .Option("header", true)
                .Option("inferSchema", true)
                .Csv(_filePath);

            //limit du dataframe avec un nombre de ligne prédéfini lors de l'appel de la fonction
            DataFrame df2 = df.Limit(nrows);

            //Watch seulement pour la résolution des sudokus
            var watch2 = new System.Diagnostics.Stopwatch();
            watch2.Start();

            // Création de la spark User Defined Function
            spark.Udf().Register<string, string>(
                "SudokuUDF",
                (sudoku) => Solution(sudoku));

            // Appel de l'UDF dans un nouveau dataframe spark qui contiendra les résultats aussi
            df2.CreateOrReplaceTempView("Resolved");
            DataFrame sqlDf = spark.Sql("SELECT  SudokuUDF(sudokus) as Resolution from Resolved");
            //DataFrame sqlDf = spark.Sql("SELECT sudokus as Resolution from Resolved");
            sqlDf.Show();

            watch2.Stop();

            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine($"Execution Time for " + nrows + " sudoku resolution : " + watch2.ElapsedMilliseconds + " ms");
            //Console.WriteLine($"Execution Time for " + nrows + " sudoku resolution with " + cores + " core and " + nodes + " instance: " + watch2.ElapsedMilliseconds + " ms");
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine();

            spark.Stop();

        }
        static void Main(string[] args)

        {

            string _filePath = @"C:\bin\Projet\5ESGF-BD-2021-main\ESGF.Sudoku.Spark.RecursiveSearch\sudoku.csv";
            //temps d'execution global (chargement du CSV + création DF et sparksession)
            var watch = new System.Diagnostics.Stopwatch();
            var watch1 = new System.Diagnostics.Stopwatch();

            //watch.Start();

            ////Appel de la méthode, spark session avec 1 noyau et 1 instance, 1000 sudokus à résoudre
            //Sudokures("1", "1", "512M", 1000);

            //watch.Stop();


            watch.Start();

            //Appel de la méthode, spark session avec 1 noyau et 1 instance, 1000 sudokus à résoudre
            Sudokures(10, _filePath);

            watch.Stop();



            //watch1.Start();

            ////Appel de la méthode, spark session avec 1 noyau et 4 instance, 1000 sudokus à résoudre
            //Sudokures("8", "24", "4G", 1000);

            //watch1.Stop();

            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine($"Global Execution (CSV + DF + SparkSession) Time: {watch.ElapsedMilliseconds} ms");
            //Console.WriteLine($"Global Execution (CSV + DF + SparkSession) Time with 4 core and 12 instances: {watch1.ElapsedMilliseconds} ms");
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine();






        }
    }


}
