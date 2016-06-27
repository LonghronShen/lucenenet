using System;
using System.Text.RegularExpressions;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Documents;
using Xunit;

namespace Lucene.Net.Search.Payloads
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     *
     *     http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    using System.IO;
    using Lucene.Net.Analysis;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using DefaultSimilarity = Lucene.Net.Search.Similarities.DefaultSimilarity;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using English = Lucene.Net.Util.English;
    using Field = Field;
    using FieldInvertState = Lucene.Net.Index.FieldInvertState;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using SpanNearQuery = Lucene.Net.Search.Spans.SpanNearQuery;
    using SpanQuery = Lucene.Net.Search.Spans.SpanQuery;
    using SpanTermQuery = Lucene.Net.Search.Spans.SpanTermQuery;
    using Term = Lucene.Net.Index.Term;

    public class TestPayloadNearQuery : LuceneTestCase, IClassFixture<TestPayloadNearQueryFixture>
    {
        private static readonly Regex _whiteSpaceRegex = new Regex("[\\s]+");
        private readonly TestPayloadNearQueryFixture _fixture;

        private PayloadNearQuery NewPhraseQuery(string fieldName, string phrase, bool inOrder, PayloadFunction function)
        {
            var words = _whiteSpaceRegex.Split(phrase);
            var clauses = new SpanQuery[words.Length];
            for (var i = 0; i < clauses.Length; i++)
            {
                clauses[i] = new SpanTermQuery(new Term(fieldName, words[i]));
            }
            return new PayloadNearQuery(clauses, 0, inOrder, function);
        }

        public TestPayloadNearQuery(TestPayloadNearQueryFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public virtual void Test()
        {
            PayloadNearQuery query;
            TopDocs hits;

            query = NewPhraseQuery("field", "twenty two", true, new AveragePayloadFunction());
            QueryUtils.Check(query);

            // all 10 hits should have score = 3 because adjacent terms have payloads of 2,4
            // and all the similarity factors are set to 1
            hits = _fixture.Searcher.Search(query, null, 100);
            Assert.True(hits != null, "hits is null and it shouldn't be");
            // 10 documents were added with the tokens "twenty two", each has 3 instances
            Assert.Equal(10, hits.TotalHits);
            for (int j = 0; j < hits.ScoreDocs.Length; j++)
            {
                ScoreDoc doc = hits.ScoreDocs[j];
                Assert.Equal(3, doc.Score);
            }
            for (int i = 1; i < 10; i++)
            {
                query = NewPhraseQuery("field", English.IntToEnglish(i) + " hundred", true, new AveragePayloadFunction());
                if (VERBOSE)
                {
                    Console.WriteLine("TEST: run query=" + query);
                }
                // all should have score = 3 because adjacent terms have payloads of 2,4
                // and all the similarity factors are set to 1
                hits = _fixture.Searcher.Search(query, null, 100);
                Assert.True(hits != null, "hits is null and it shouldn't be");
                Assert.Equal(100, hits.TotalHits);
                for (int j = 0; j < hits.ScoreDocs.Length; j++)
                {
                    ScoreDoc doc = hits.ScoreDocs[j];
                    //        System.out.println("Doc: " + doc.toString());
                    //        System.out.println("Explain: " + searcher.Explain(query, doc.Doc));
                    Assert.Equal(3, doc.Score);
                }
            }
        }

        [Fact]
        public virtual void TestPayloadNear()
        {
            SpanNearQuery q1, q2;
            PayloadNearQuery query;
            //SpanNearQuery(clauses, 10000, false)
            q1 = SpanNearQuery("field2", "twenty two");
            q2 = SpanNearQuery("field2", "twenty three");
            SpanQuery[] clauses = new SpanQuery[2];
            clauses[0] = q1;
            clauses[1] = q2;
            query = new PayloadNearQuery(clauses, 10, false);
            //System.out.println(query.toString());
            Assert.Equal(12, _fixture.Searcher.Search(query, null, 100).TotalHits);
            /*
            System.out.println(hits.TotalHits);
            for (int j = 0; j < hits.ScoreDocs.Length; j++) {
              ScoreDoc doc = hits.ScoreDocs[j];
              System.out.println("doc: "+doc.Doc+", score: "+doc.Score);
            }
            */
        }

        [Fact]
        public virtual void TestAverageFunction()
        {
            PayloadNearQuery query;
            TopDocs hits;

            query = NewPhraseQuery("field", "twenty two", true, new AveragePayloadFunction());
            QueryUtils.Check(query);
            // all 10 hits should have score = 3 because adjacent terms have payloads of 2,4
            // and all the similarity factors are set to 1
            hits = _fixture.Searcher.Search(query, null, 100);
            Assert.True(hits != null, "hits is null and it shouldn't be");
            Assert.Equal(10, hits.TotalHits);
            for (int j = 0; j < hits.ScoreDocs.Length; j++)
            {
                ScoreDoc doc = hits.ScoreDocs[j];
                Assert.Equal(3, doc.Score);
                Explanation explain = _fixture.Searcher.Explain(query, hits.ScoreDocs[j].Doc);
                string exp = explain.ToString();
                Assert.True(exp.IndexOf("AveragePayloadFunction") > -1, exp);
                Assert.Equal(3f, explain.Value); //, hits.ScoreDocs[j].Score + " explain value does not equal: " + 3);
            }
        }

        [Fact]
        public virtual void TestMaxFunction()
        {
            PayloadNearQuery query;
            TopDocs hits;

            query = NewPhraseQuery("field", "twenty two", true, new MaxPayloadFunction());
            QueryUtils.Check(query);
            // all 10 hits should have score = 4 (max payload value)
            hits = _fixture.Searcher.Search(query, null, 100);
            Assert.True(hits != null, "hits is null and it shouldn't be");
            Assert.Equal(10, hits.TotalHits);
            for (int j = 0; j < hits.ScoreDocs.Length; j++)
            {
                ScoreDoc doc = hits.ScoreDocs[j];
                Assert.Equal(4, doc.Score);
                Explanation explain = _fixture.Searcher.Explain(query, hits.ScoreDocs[j].Doc);
                string exp = explain.ToString();
                Assert.True(exp.IndexOf("MaxPayloadFunction") > -1, exp);
                Assert.Equal(4f, explain.Value); //, hits.ScoreDocs[j].Score + " explain value does not equal: " + 4);
            }
        }

        [Fact]
        public virtual void TestMinFunction()
        {
            PayloadNearQuery query;
            TopDocs hits;

            query = NewPhraseQuery("field", "twenty two", true, new MinPayloadFunction());
            QueryUtils.Check(query);
            // all 10 hits should have score = 2 (min payload value)
            hits = _fixture.Searcher.Search(query, null, 100);
            Assert.True(hits != null, "hits is null and it shouldn't be");
            Assert.Equal(10, hits.TotalHits);
            for (int j = 0; j < hits.ScoreDocs.Length; j++)
            {
                ScoreDoc doc = hits.ScoreDocs[j];
                Assert.Equal(2, doc.Score);
                Explanation explain = _fixture.Searcher.Explain(query, hits.ScoreDocs[j].Doc);
                string exp = explain.ToString();
                Assert.True(exp.IndexOf("MinPayloadFunction") > -1, exp);
                Assert.Equal(2f, explain.Value); //, hits.ScoreDocs[j].Score + " explain value does not equal: " + 2);
            }
        }

        private SpanQuery[] Clauses
        {
            get
            {
                SpanNearQuery q1, q2;
                q1 = SpanNearQuery("field2", "twenty two");
                q2 = SpanNearQuery("field2", "twenty three");
                SpanQuery[] clauses = new SpanQuery[2];
                clauses[0] = q1;
                clauses[1] = q2;
                return clauses;
            }
        }

        private SpanNearQuery SpanNearQuery(string fieldName, string words)
        {
            var wordList = _whiteSpaceRegex.Split(words);
            var clauses = new SpanQuery[wordList.Length];
            for (var i = 0; i < clauses.Length; i++)
            {
                clauses[i] = new PayloadTermQuery(new Term(fieldName, wordList[i]), new AveragePayloadFunction());
            }
            return new SpanNearQuery(clauses, 10000, false);
        }

        [Fact]
        public virtual void TestLongerSpan()
        {
            PayloadNearQuery query;
            TopDocs hits;
            query = NewPhraseQuery("field", "nine hundred ninety nine", true, new AveragePayloadFunction());
            hits = _fixture.Searcher.Search(query, null, 100);
            Assert.True(hits != null, "hits is null and it shouldn't be");
            ScoreDoc doc = hits.ScoreDocs[0];
            //    System.out.println("Doc: " + doc.toString());
            //    System.out.println("Explain: " + searcher.Explain(query, doc.Doc));
            Assert.True(hits.TotalHits == 1, "there should only be one hit");
            // should have score = 3 because adjacent terms have payloads of 2,4
            Assert.Equal(3, doc.Score);
        }

        [Fact]
        public virtual void TestComplexNested()
        {
            PayloadNearQuery query;
            TopDocs hits;

            // combine ordered and unordered spans with some nesting to make sure all payloads are counted

            SpanQuery q1 = NewPhraseQuery("field", "nine hundred", true, new AveragePayloadFunction());
            SpanQuery q2 = NewPhraseQuery("field", "ninety nine", true, new AveragePayloadFunction());
            SpanQuery q3 = NewPhraseQuery("field", "nine ninety", false, new AveragePayloadFunction());
            SpanQuery q4 = NewPhraseQuery("field", "hundred nine", false, new AveragePayloadFunction());
            SpanQuery[] clauses = new SpanQuery[] { new PayloadNearQuery(new SpanQuery[] { q1, q2 }, 0, true), new PayloadNearQuery(new SpanQuery[] { q3, q4 }, 0, false) };
            query = new PayloadNearQuery(clauses, 0, false);
            hits = _fixture.Searcher.Search(query, null, 100);
            Assert.True(hits != null, "hits is null and it shouldn't be");
            // should be only 1 hit - doc 999
            Assert.True(hits.ScoreDocs.Length == 1, "should only be one hit");
            // the score should be 3 - the average of all the underlying payloads
            ScoreDoc doc = hits.ScoreDocs[0];
            //    System.out.println("Doc: " + doc.toString());
            //    System.out.println("Explain: " + searcher.Explain(query, doc.Doc));
            Assert.True(doc.Score == 3, doc.Score + " does not equal: " + 3);
        }
    }

    public class TestPayloadNearQueryFixture : IDisposable
    {
        internal IndexSearcher Searcher { get; private set; }
        internal IndexReader Reader { get; private set; }
        internal Directory Directory { get; private set; }

        internal static readonly byte[] Payload2 = { 2 };
        internal static readonly byte[] Payload4 = { 4 };

        public TestPayloadNearQueryFixture()
        {
            var similarity = new BoostingSimilarity();
            Directory = LuceneTestCase.NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(LuceneTestCase.Random(), Directory, LuceneTestCase.NewIndexWriterConfig(LuceneTestCase.TEST_VERSION_CURRENT, new PayloadAnalyzer()).SetSimilarity(similarity));
            //writer.infoStream = System.out;
            for (int i = 0; i < 1000; i++)
            {
                Document doc = new Document();
                doc.Add(LuceneTestCase.NewTextField("field", English.IntToEnglish(i), Field.Store.YES));
                string txt = English.IntToEnglish(i) + ' ' + English.IntToEnglish(i + 1);
                doc.Add(LuceneTestCase.NewTextField("field2", txt, Field.Store.YES));
                writer.AddDocument(doc);
            }
            Reader = writer.Reader;
            writer.Dispose();

            Searcher = LuceneTestCase.NewSearcher(Reader);
            Searcher.Similarity = similarity;
        }

        public void Dispose()
        {
            Searcher = null;
            Reader.Dispose();
            Reader = null;
            Directory.Dispose();
            Directory = null;
        }

        private class PayloadAnalyzer : Analyzer
        {
            public override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                Tokenizer result = new MockTokenizer(reader, MockTokenizer.SIMPLE, true);
                return new TokenStreamComponents(result, new PayloadFilter(result, fieldName));
            }
        }

        private class PayloadFilter : TokenFilter
        {
            internal readonly string FieldName;
            internal int NumSeen = 0;
            internal readonly IPayloadAttribute PayAtt;

            public PayloadFilter(TokenStream input, string fieldName)
                : base(input)
            {
                this.FieldName = fieldName;
                PayAtt = AddAttribute<IPayloadAttribute>();
            }

            public sealed override bool IncrementToken()
            {
                bool result = false;
                if (input.IncrementToken())
                {
                    if (NumSeen % 2 == 0)
                    {
                        PayAtt.Payload = new BytesRef(Payload2);
                    }
                    else
                    {
                        PayAtt.Payload = new BytesRef(Payload4);
                    }
                    NumSeen++;
                    result = true;
                }
                return result;
            }

            public override void Reset()
            {
                base.Reset();
                this.NumSeen = 0;
            }
        }

        private class BoostingSimilarity : DefaultSimilarity
        {
            public override float QueryNorm(float sumOfSquaredWeights)
            {
                return 1.0f;
            }

            public override float Coord(int overlap, int maxOverlap)
            {
                return 1.0f;
            }

            public override float ScorePayload(int docId, int start, int end, BytesRef payload)
            {
                //we know it is size 4 here, so ignore the offset/length
                return payload.Bytes[payload.Offset];
            }

            //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            //Make everything else 1 so we see the effect of the payload
            //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            public override float LengthNorm(FieldInvertState state)
            {
                return state.Boost;
            }

            public override float SloppyFreq(int distance)
            {
                return 1.0f;
            }

            public override float Tf(float freq)
            {
                return 1.0f;
            }

            // idf used for phrase queries
            public override Explanation IdfExplain(CollectionStatistics collectionStats, TermStatistics[] termStats)
            {
                return new Explanation(1.0f, "Inexplicable");
            }
        }
    }

}