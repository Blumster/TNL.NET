using System;
using System.Security.Cryptography;

namespace TNL.NET.Structs
{
    public enum ErrorCode
    {
        Success,
        InvalidSolution,
        InvalidServerNonce,
        InvalidClientNonce,
        InvalidPuzzleDifficulty,
        ErrorCodeCount,
    };

    public class ClientPuzzleManager
    {
        public const UInt32 PuzzleRefreshTime = 30000;
        public const UInt32 InitialPuzzleDifficulty = 17;
        public const UInt32 MaxPuzzleDifficulty = 26;
        public const UInt32 MaxSolutionComputeFragment = 30;
        public const UInt32 SolutionFragmentIterations = 50000;

        public UInt32 CurrentDifficulty { get; private set; }
        public Int32 LastUpdateTime { get; private set; }
        public Int32 LastTickTime { get; private set; }
        public Nonce CurrentNonce { get; private set; }
        public Nonce LastNonce { get; private set; }

        public ClientPuzzleManager()
        {
            CurrentDifficulty = InitialPuzzleDifficulty;
            LastUpdateTime = 0;
            LastTickTime = 0;

            CurrentNonce = new Nonce();
            CurrentNonce.GetRandom();

            LastNonce = new Nonce();
            LastNonce.GetRandom();
        }

        public void Tick(Int32 currentTime)
        {
            if (LastTickTime == 0)
                LastTickTime = currentTime;

            var delta = currentTime - LastUpdateTime;
            if (delta <= PuzzleRefreshTime)
                return;

            LastUpdateTime = currentTime;
            LastNonce = CurrentNonce;

            CurrentNonce.GetRandom();
        }

        public ErrorCode CheckSolution(UInt32 solution, Nonce clientNonce, Nonce serverNonce, UInt32 puzzleDifficulty, UInt32 clientIdentity)
        {
            if (puzzleDifficulty != CurrentDifficulty)
                return ErrorCode.InvalidPuzzleDifficulty;

            return CheckOneSolution(solution, clientNonce, serverNonce, puzzleDifficulty, clientIdentity) ? ErrorCode.Success : ErrorCode.InvalidSolution;
        }

        public static Boolean CheckOneSolution(UInt32 solution, Nonce clientNonce, Nonce serverNonce, UInt32 puzzleDifficulty, UInt32 clientIdentity)
        {
            var buffer = new Byte[24];

            var sol = BitConverter.GetBytes(solution);
            Array.Reverse(sol);

            var cid = BitConverter.GetBytes(clientIdentity);
            Array.Reverse(cid);

            Array.Copy(sol, 0, buffer, 0, 4);
            Array.Copy(cid, 0, buffer, 4, 4);
            Array.Copy(clientNonce.Data, 0, buffer, 8, 8);
            Array.Copy(serverNonce.Data, 0, buffer, 16, 8);

            var hash = new SHA256Managed().ComputeHash(buffer);

            var index = 0U;
            while (puzzleDifficulty > 8)
            {
                if (hash[index] != 0)
                    return false;

                ++index;
                puzzleDifficulty -= 8;
            }

            return (hash[index] & (0xFF << (8 - (Int32)puzzleDifficulty))) == 0;
        }

        public static Boolean SolvePuzzle(ref UInt32 solution, Nonce clientNonce, Nonce serverNonce, UInt32 puzzleDifficulty, UInt32 clientIdentity)
        {
            var startTime = Environment.TickCount;
            var startValue = solution;

            while (true)
            {
                var nextValue = startValue + SolutionFragmentIterations;
                for (; startValue < nextValue; ++startValue)
                {
                    if (!CheckOneSolution(startValue, clientNonce, serverNonce, puzzleDifficulty, clientIdentity))
                        continue;

                    solution = startValue;
                    return true;
                }

                if (Environment.TickCount - startTime <= MaxSolutionComputeFragment)
                    continue;

                solution = startValue;
                return false;
            }
        }
    }
}
