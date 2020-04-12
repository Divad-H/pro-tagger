using System;
using System.Linq;
using System.Security.Cryptography;

namespace ProTaggerTest.LibGit2Mocks
{
    class PseudoShaGenerator
    {
        private readonly SHA1 _sha = new SHA1CryptoServiceProvider();
        
        private readonly Random _random = new Random();
        public string Generate() => BitConverter.ToString(_sha.ComputeHash(Enumerable
                                                     .Repeat(0, 6)
                                                     .Select(_ => (byte)_random.Next(byte.MaxValue + 1))
                                                     .ToArray()))
                                                .Replace("-", "")
                                                .ToLower();
    }
}
