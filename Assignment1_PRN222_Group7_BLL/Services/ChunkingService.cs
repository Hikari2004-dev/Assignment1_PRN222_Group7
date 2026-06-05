using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Assignment1_PRN222_Group7_DAL.Enums;

namespace Assignment1_PRN222_Group7_BLL.Services
{
    public class ChunkingService : IChunkingService
    {
        public List<string> ChunkText(string text, ChunkingStrategy strategy = ChunkingStrategy.Fixed, int chunkSize = 500, int overlap = 50)
        {
            if (string.IsNullOrWhiteSpace(text)) return [];

            return strategy switch
            {
                ChunkingStrategy.Sentence => ChunkTextSentence(text, chunkSize, overlap),
                ChunkingStrategy.Recursive => ChunkTextRecursive(text, chunkSize, overlap),
                ChunkingStrategy.Semantic => ChunkTextSemantic(text, chunkSize, overlap),
                _ => ChunkTextFixed(text, chunkSize, overlap)
            };
        }

        /// <summary>
        /// Fixed sliding window chunking by word count.
        /// </summary>
        private List<string> ChunkTextFixed(string text, int chunkSize, int overlap)
        {
            var words = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0) return [];

            var chunks = new List<string>();
            int step = Math.Max(1, chunkSize - overlap);

            for (int i = 0; i < words.Length; i += step)
            {
                int end = Math.Min(i + chunkSize, words.Length);
                chunks.Add(string.Join(' ', words[i..end]));
                if (end == words.Length) break;
            }

            return chunks;
        }

        /// <summary>
        /// Split into sentences using delimiters, and group them to fit chunkSize words.
        /// </summary>
        private List<string> ChunkTextSentence(string text, int chunkSize, int overlap)
        {
            // Split into sentences, keeping the punctuation
            var sentences = Regex.Split(text, @"(?<=[.!?])\s+");
            var chunks = new List<string>();
            var currentChunk = new List<string>();
            int currentWordCount = 0;

            for (int i = 0; i < sentences.Length; i++)
            {
                var sentence = sentences[i].Trim();
                if (string.IsNullOrWhiteSpace(sentence)) continue;

                var sentenceWords = sentence.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                int sentenceWordCount = sentenceWords.Length;

                if (currentWordCount + sentenceWordCount > chunkSize && currentChunk.Any())
                {
                    chunks.Add(string.Join(' ', currentChunk));

                    // Overlap: carry over the last sentences whose word count is <= overlap
                    var overlapChunk = new List<string>();
                    int overlapCount = 0;
                    for (int j = currentChunk.Count - 1; j >= 0; j--)
                    {
                        var prevSentence = currentChunk[j];
                        int prevWordCount = prevSentence.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
                        if (overlapCount + prevWordCount <= overlap)
                        {
                            overlapChunk.Insert(0, prevSentence);
                            overlapCount += prevWordCount;
                        }
                        else
                        {
                            break;
                        }
                    }

                    currentChunk = overlapChunk;
                    currentWordCount = overlapCount;
                }

                currentChunk.Add(sentence);
                currentWordCount += sentenceWordCount;
            }

            if (currentChunk.Any())
            {
                chunks.Add(string.Join(' ', currentChunk));
            }

            return chunks;
        }

        /// <summary>
        /// Recursively split text on paragraphs, newlines, sentences, and words to fit chunkSize.
        /// </summary>
        private List<string> ChunkTextRecursive(string text, int chunkSize, int overlap)
        {
            var separators = new[] { "\r\n\r\n", "\n\n", "\r\n", "\n", ". ", "? ", "! ", " ", "" };
            var pieces = RecursiveSplit(text, separators, 0, chunkSize);
            return MergePieces(pieces, chunkSize, overlap);
        }

        private List<string> RecursiveSplit(string text, string[] separators, int sepIndex, int maxWords)
        {
            var words = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length <= maxWords)
            {
                return new List<string> { text };
            }

            if (sepIndex >= separators.Length)
            {
                var list = new List<string>();
                for (int i = 0; i < words.Length; i += maxWords)
                {
                    int len = Math.Min(maxWords, words.Length - i);
                    list.Add(string.Join(' ', words[i..(i + len)]));
                }
                return list;
            }

            var separator = separators[sepIndex];
            var splits = string.IsNullOrEmpty(separator)
                ? text.Select(c => c.ToString()).ToArray()
                : text.Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries);

            var finalPieces = new List<string>();
            foreach (var part in splits)
            {
                if (string.IsNullOrWhiteSpace(part)) continue;
                var partWords = part.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                if (partWords.Length <= maxWords)
                {
                    finalPieces.Add(part);
                }
                else
                {
                    finalPieces.AddRange(RecursiveSplit(part, separators, sepIndex + 1, maxWords));
                }
            }

            return finalPieces;
        }

        /// <summary>
        /// Split by section boundaries (headers starting with #, Chapter, etc.) and group sections.
        /// </summary>
        private List<string> ChunkTextSemantic(string text, int chunkSize, int overlap)
        {
            var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            var sections = new List<string>();
            var currentSection = new List<string>();

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                bool isHeader = trimmed.StartsWith('#') || 
                                trimmed.StartsWith("Chapter", StringComparison.OrdinalIgnoreCase) ||
                                trimmed.StartsWith("Chương", StringComparison.OrdinalIgnoreCase) ||
                                trimmed.StartsWith("Section", StringComparison.OrdinalIgnoreCase) ||
                                trimmed.StartsWith("Mục", StringComparison.OrdinalIgnoreCase) ||
                                Regex.IsMatch(trimmed, @"^[I|V|X|L|C|D|M]+\.\s+") ||
                                Regex.IsMatch(trimmed, @"^\d+(\.\d+)*\.\s+");

                if (isHeader && currentSection.Any())
                {
                    sections.Add(string.Join('\n', currentSection));
                    currentSection.Clear();
                }

                currentSection.Add(line);
            }

            if (currentSection.Any())
            {
                sections.Add(string.Join('\n', currentSection));
            }

            return MergePieces(sections, chunkSize, overlap);
        }

        private List<string> MergePieces(List<string> pieces, int chunkSize, int overlap)
        {
            var chunks = new List<string>();
            var currentChunk = new List<string>();
            int currentWordCount = 0;

            for (int i = 0; i < pieces.Count; i++)
            {
                var piece = pieces[i];
                var pieceWords = piece.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                int pieceWordCount = pieceWords.Length;

                if (currentWordCount + pieceWordCount > chunkSize && currentChunk.Any())
                {
                    chunks.Add(string.Join('\n', currentChunk));

                    // Overlap: carry over the last pieces up to the overlap word count
                    var overlapChunk = new List<string>();
                    int overlapCount = 0;
                    for (int j = currentChunk.Count - 1; j >= 0; j--)
                    {
                        var prevPiece = currentChunk[j];
                        int prevWordCount = prevPiece.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
                        if (overlapCount + prevWordCount <= overlap)
                        {
                            overlapChunk.Insert(0, prevPiece);
                            overlapCount += prevWordCount;
                        }
                        else
                        {
                            break;
                        }
                    }

                    currentChunk = overlapChunk;
                    currentWordCount = overlapCount;
                }

                currentChunk.Add(piece);
                currentWordCount += pieceWordCount;
            }

            if (currentChunk.Any())
            {
                chunks.Add(string.Join('\n', currentChunk));
            }

            return chunks;
        }
    }
}
