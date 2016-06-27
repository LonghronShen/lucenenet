using System;
using System.Globalization;
using Lucene.Net.Documents;
using Lucene.Net.Randomized.Generators;
using Xunit;

namespace Lucene.Net.Search
{
    using DefaultSimilarity = Lucene.Net.Search.Similarities.DefaultSimilarity;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;

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

    using Field = Field;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using Similarity = Lucene.Net.Search.Similarities.Similarity;
    using Term = Lucene.Net.Index.Term;

    /// <summary>
    /// Test that BooleanQuery.setMinimumNumberShouldMatch works.
    /// </summary>
    public class TestBooleanMinShouldMatch : LuceneTestCase, IClassFixture<TestBooleanMinShouldMatchFixture>
    {
        private readonly TestBooleanMinShouldMatchFixture _fixture;

        public TestBooleanMinShouldMatch(TestBooleanMinShouldMatchFixture fixture)
        {
            _fixture = fixture;
        }

        public virtual void VerifyNrHits(Query q, int expected)
        {
            // bs1
            ScoreDoc[] h = _fixture.IndexSearcher.Search(q, null, 1000).ScoreDocs;
            if (expected != h.Length)
            {
                PrintHits(TestName, h, _fixture.IndexSearcher);
            }
            Assert.Equal(expected, h.Length); //, "result count");
            //System.out.println("TEST: now check");
            // bs2
            TopScoreDocCollector collector = TopScoreDocCollector.Create(1000, true);
            _fixture.IndexSearcher.Search(q, collector);
            ScoreDoc[] h2 = collector.TopDocs().ScoreDocs;
            if (expected != h2.Length)
            {
                PrintHits(TestName, h2, _fixture.IndexSearcher);
            }
            Assert.Equal(expected, h2.Length); //, "result count (bs2)");

            QueryUtils.Check(Random(), q, _fixture.IndexSearcher);
        }

        [Fact]
        public virtual void TestAllOptional()
        {
            BooleanQuery q = new BooleanQuery();
            for (int i = 1; i <= 4; i++)
            {
                q.Add(new TermQuery(new Term("data", "" + i)), BooleanClause.Occur.SHOULD); //false, false);
            }
            q.MinimumNumberShouldMatch = 2; // match at least two of 4
            VerifyNrHits(q, 2);
        }

        [Fact]
        public virtual void TestOneReqAndSomeOptional()
        {
            /* one required, some optional */
            BooleanQuery q = new BooleanQuery();
            q.Add(new TermQuery(new Term("all", "all")), BooleanClause.Occur.MUST); //true,  false);
            q.Add(new TermQuery(new Term("data", "5")), BooleanClause.Occur.SHOULD); //false, false);
            q.Add(new TermQuery(new Term("data", "4")), BooleanClause.Occur.SHOULD); //false, false);
            q.Add(new TermQuery(new Term("data", "3")), BooleanClause.Occur.SHOULD); //false, false);

            q.MinimumNumberShouldMatch = 2; // 2 of 3 optional

            VerifyNrHits(q, 5);
        }

        [Fact]
        public virtual void TestSomeReqAndSomeOptional()
        {
            /* two required, some optional */
            BooleanQuery q = new BooleanQuery();
            q.Add(new TermQuery(new Term("all", "all")), BooleanClause.Occur.MUST); //true,  false);
            q.Add(new TermQuery(new Term("data", "6")), BooleanClause.Occur.MUST); //true,  false);
            q.Add(new TermQuery(new Term("data", "5")), BooleanClause.Occur.SHOULD); //false, false);
            q.Add(new TermQuery(new Term("data", "4")), BooleanClause.Occur.SHOULD); //false, false);
            q.Add(new TermQuery(new Term("data", "3")), BooleanClause.Occur.SHOULD); //false, false);

            q.MinimumNumberShouldMatch = 2; // 2 of 3 optional

            VerifyNrHits(q, 5);
        }

        [Fact]
        public virtual void TestOneProhibAndSomeOptional()
        {
            /* one prohibited, some optional */
            BooleanQuery q = new BooleanQuery();
            q.Add(new TermQuery(new Term("data", "1")), BooleanClause.Occur.SHOULD); //false, false);
            q.Add(new TermQuery(new Term("data", "2")), BooleanClause.Occur.SHOULD); //false, false);
            q.Add(new TermQuery(new Term("data", "3")), BooleanClause.Occur.MUST_NOT); //false, true );
            q.Add(new TermQuery(new Term("data", "4")), BooleanClause.Occur.SHOULD); //false, false);

            q.MinimumNumberShouldMatch = 2; // 2 of 3 optional

            VerifyNrHits(q, 1);
        }

        [Fact]
        public virtual void TestSomeProhibAndSomeOptional()
        {
            /* two prohibited, some optional */
            BooleanQuery q = new BooleanQuery();
            q.Add(new TermQuery(new Term("data", "1")), BooleanClause.Occur.SHOULD); //false, false);
            q.Add(new TermQuery(new Term("data", "2")), BooleanClause.Occur.SHOULD); //false, false);
            q.Add(new TermQuery(new Term("data", "3")), BooleanClause.Occur.MUST_NOT); //false, true );
            q.Add(new TermQuery(new Term("data", "4")), BooleanClause.Occur.SHOULD); //false, false);
            q.Add(new TermQuery(new Term("data", "C")), BooleanClause.Occur.MUST_NOT); //false, true );

            q.MinimumNumberShouldMatch = 2; // 2 of 3 optional

            VerifyNrHits(q, 1);
        }

        [Fact]
        public virtual void TestOneReqOneProhibAndSomeOptional()
        {
            /* one required, one prohibited, some optional */
            BooleanQuery q = new BooleanQuery();
            q.Add(new TermQuery(new Term("data", "6")), BooleanClause.Occur.MUST); // true,  false);
            q.Add(new TermQuery(new Term("data", "5")), BooleanClause.Occur.SHOULD); //false, false);
            q.Add(new TermQuery(new Term("data", "4")), BooleanClause.Occur.SHOULD); //false, false);
            q.Add(new TermQuery(new Term("data", "3")), BooleanClause.Occur.MUST_NOT); //false, true );
            q.Add(new TermQuery(new Term("data", "2")), BooleanClause.Occur.SHOULD); //false, false);
            q.Add(new TermQuery(new Term("data", "1")), BooleanClause.Occur.SHOULD); //false, false);

            q.MinimumNumberShouldMatch = 3; // 3 of 4 optional

            VerifyNrHits(q, 1);
        }

        [Fact]
        public virtual void TestSomeReqOneProhibAndSomeOptional()
        {
            /* two required, one prohibited, some optional */
            BooleanQuery q = new BooleanQuery();
            q.Add(new TermQuery(new Term("all", "all")), BooleanClause.Occur.MUST); //true,  false);
            q.Add(new TermQuery(new Term("data", "6")), BooleanClause.Occur.MUST); //true,  false);
            q.Add(new TermQuery(new Term("data", "5")), BooleanClause.Occur.SHOULD); //false, false);
            q.Add(new TermQuery(new Term("data", "4")), BooleanClause.Occur.SHOULD); //false, false);
            q.Add(new TermQuery(new Term("data", "3")), BooleanClause.Occur.MUST_NOT); //false, true );
            q.Add(new TermQuery(new Term("data", "2")), BooleanClause.Occur.SHOULD); //false, false);
            q.Add(new TermQuery(new Term("data", "1")), BooleanClause.Occur.SHOULD); //false, false);

            q.MinimumNumberShouldMatch = 3; // 3 of 4 optional

            VerifyNrHits(q, 1);
        }

        [Fact]
        public virtual void TestOneReqSomeProhibAndSomeOptional()
        {
            /* one required, two prohibited, some optional */
            BooleanQuery q = new BooleanQuery();
            q.Add(new TermQuery(new Term("data", "6")), BooleanClause.Occur.MUST); //true,  false);
            q.Add(new TermQuery(new Term("data", "5")), BooleanClause.Occur.SHOULD); //false, false);
            q.Add(new TermQuery(new Term("data", "4")), BooleanClause.Occur.SHOULD); //false, false);
            q.Add(new TermQuery(new Term("data", "3")), BooleanClause.Occur.MUST_NOT); //false, true );
            q.Add(new TermQuery(new Term("data", "2")), BooleanClause.Occur.SHOULD); //false, false);
            q.Add(new TermQuery(new Term("data", "1")), BooleanClause.Occur.SHOULD); //false, false);
            q.Add(new TermQuery(new Term("data", "C")), BooleanClause.Occur.MUST_NOT); //false, true );

            q.MinimumNumberShouldMatch = 3; // 3 of 4 optional

            VerifyNrHits(q, 1);
        }

        [Fact]
        public virtual void TestSomeReqSomeProhibAndSomeOptional()
        {
            /* two required, two prohibited, some optional */
            BooleanQuery q = new BooleanQuery();
            q.Add(new TermQuery(new Term("all", "all")), BooleanClause.Occur.MUST); //true,  false);
            q.Add(new TermQuery(new Term("data", "6")), BooleanClause.Occur.MUST); //true,  false);
            q.Add(new TermQuery(new Term("data", "5")), BooleanClause.Occur.SHOULD); //false, false);
            q.Add(new TermQuery(new Term("data", "4")), BooleanClause.Occur.SHOULD); //false, false);
            q.Add(new TermQuery(new Term("data", "3")), BooleanClause.Occur.MUST_NOT); //false, true );
            q.Add(new TermQuery(new Term("data", "2")), BooleanClause.Occur.SHOULD); //false, false);
            q.Add(new TermQuery(new Term("data", "1")), BooleanClause.Occur.SHOULD); //false, false);
            q.Add(new TermQuery(new Term("data", "C")), BooleanClause.Occur.MUST_NOT); //false, true );

            q.MinimumNumberShouldMatch = 3; // 3 of 4 optional

            VerifyNrHits(q, 1);
        }

        [Fact]
        public virtual void TestMinHigherThenNumOptional()
        {
            /* two required, two prohibited, some optional */
            BooleanQuery q = new BooleanQuery();
            q.Add(new TermQuery(new Term("all", "all")), BooleanClause.Occur.MUST); //true,  false);
            q.Add(new TermQuery(new Term("data", "6")), BooleanClause.Occur.MUST); //true,  false);
            q.Add(new TermQuery(new Term("data", "5")), BooleanClause.Occur.SHOULD); //false, false);
            q.Add(new TermQuery(new Term("data", "4")), BooleanClause.Occur.SHOULD); //false, false);
            q.Add(new TermQuery(new Term("data", "3")), BooleanClause.Occur.MUST_NOT); //false, true );
            q.Add(new TermQuery(new Term("data", "2")), BooleanClause.Occur.SHOULD); //false, false);
            q.Add(new TermQuery(new Term("data", "1")), BooleanClause.Occur.SHOULD); //false, false);
            q.Add(new TermQuery(new Term("data", "C")), BooleanClause.Occur.MUST_NOT); //false, true );

            q.MinimumNumberShouldMatch = 90; // 90 of 4 optional ?!?!?!

            VerifyNrHits(q, 0);
        }

        [Fact]
        public virtual void TestMinEqualToNumOptional()
        {
            /* two required, two optional */
            BooleanQuery q = new BooleanQuery();
            q.Add(new TermQuery(new Term("all", "all")), BooleanClause.Occur.SHOULD); //false, false);
            q.Add(new TermQuery(new Term("data", "6")), BooleanClause.Occur.MUST); //true,  false);
            q.Add(new TermQuery(new Term("data", "3")), BooleanClause.Occur.MUST); //true,  false);
            q.Add(new TermQuery(new Term("data", "2")), BooleanClause.Occur.SHOULD); //false, false);

            q.MinimumNumberShouldMatch = 2; // 2 of 2 optional

            VerifyNrHits(q, 1);
        }

        [Fact]
        public virtual void TestOneOptionalEqualToMin()
        {
            /* two required, one optional */
            BooleanQuery q = new BooleanQuery();
            q.Add(new TermQuery(new Term("all", "all")), BooleanClause.Occur.MUST); //true,  false);
            q.Add(new TermQuery(new Term("data", "3")), BooleanClause.Occur.SHOULD); //false, false);
            q.Add(new TermQuery(new Term("data", "2")), BooleanClause.Occur.MUST); //true,  false);

            q.MinimumNumberShouldMatch = 1; // 1 of 1 optional

            VerifyNrHits(q, 1);
        }

        [Fact]
        public virtual void TestNoOptionalButMin()
        {
            /* two required, no optional */
            BooleanQuery q = new BooleanQuery();
            q.Add(new TermQuery(new Term("all", "all")), BooleanClause.Occur.MUST); //true,  false);
            q.Add(new TermQuery(new Term("data", "2")), BooleanClause.Occur.MUST); //true,  false);

            q.MinimumNumberShouldMatch = 1; // 1 of 0 optional

            VerifyNrHits(q, 0);
        }

        [Fact]
        public virtual void TestNoOptionalButMin2()
        {
            /* one required, no optional */
            BooleanQuery q = new BooleanQuery();
            q.Add(new TermQuery(new Term("all", "all")), BooleanClause.Occur.MUST); //true,  false);

            q.MinimumNumberShouldMatch = 1; // 1 of 0 optional

            VerifyNrHits(q, 0);
        }

        [Fact]
        public virtual void TestRandomQueries()
        {
            const string field = "data";
            string[] vals = new string[] { "1", "2", "3", "4", "5", "6", "A", "Z", "B", "Y", "Z", "X", "foo" };
            int maxLev = 4;

            // callback object to set a random setMinimumNumberShouldMatch
            TestBoolean2.Callback minNrCB = new CallbackAnonymousInnerClassHelper(this, field, vals);

            // increase number of iterations for more complete testing
            int num = AtLeast(20);
            for (int i = 0; i < num; i++)
            {
                int lev = Random().Next(maxLev);
                int seed = Random().Next();
                BooleanQuery q1 = TestBoolean2.RandBoolQuery(new Random(seed), true, lev, field, vals, null);
                // BooleanQuery q2 = TestBoolean2.randBoolQuery(new Random(seed), lev, field, vals, minNrCB);
                BooleanQuery q2 = TestBoolean2.RandBoolQuery(new Random(seed), true, lev, field, vals, null);
                // only set minimumNumberShouldMatch on the top level query since setting
                // at a lower level can change the score.
                minNrCB.PostCreate(q2);

                // Can't use Hits because normalized scores will mess things
                // up.  The non-sorting version of search() that returns TopDocs
                // will not normalize scores.
                TopDocs top1 = _fixture.IndexSearcher.Search(q1, null, 100);
                TopDocs top2 = _fixture.IndexSearcher.Search(q2, null, 100);
                if (i < 100)
                {
                    QueryUtils.Check(Random(), q1, _fixture.IndexSearcher);
                    QueryUtils.Check(Random(), q2, _fixture.IndexSearcher);
                }
                AssertSubsetOfSameScores(q2, top1, top2);
            }
            // System.out.println("Total hits:"+tot);
        }

        private class CallbackAnonymousInnerClassHelper : TestBoolean2.Callback
        {
            private readonly TestBooleanMinShouldMatch OuterInstance;

            private string Field;
            private string[] Vals;

            public CallbackAnonymousInnerClassHelper(TestBooleanMinShouldMatch outerInstance, string field, string[] vals)
            {
                this.OuterInstance = outerInstance;
                this.Field = field;
                this.Vals = vals;
            }

            public virtual void PostCreate(BooleanQuery q)
            {
                BooleanClause[] c = q.Clauses;
                int opt = 0;
                for (int i = 0; i < c.Length; i++)
                {
                    if (c[i].Occur_ == BooleanClause.Occur.SHOULD)
                    {
                        opt++;
                    }
                }
                q.MinimumNumberShouldMatch = Random().Next(opt + 2);
                if (Random().NextBoolean())
                {
                    // also add a random negation
                    Term randomTerm = new Term(Field, Vals[Random().Next(Vals.Length)]);
                    q.Add(new TermQuery(randomTerm), BooleanClause.Occur.MUST_NOT);
                }
            }
        }

        private void AssertSubsetOfSameScores(Query q, TopDocs top1, TopDocs top2)
        {
            // The constrained query
            // should be a subset to the unconstrained query.
            if (top2.TotalHits > top1.TotalHits)
            {
                Assert.True(false, "Constrained results not a subset:\n" + CheckHits.TopdocsString(top1, 0, 0) + CheckHits.TopdocsString(top2, 0, 0) + "for query:" + q.ToString());
            }

            for (int hit = 0; hit < top2.TotalHits; hit++)
            {
                int id = top2.ScoreDocs[hit].Doc;
                float score = top2.ScoreDocs[hit].Score;
                bool found = false;
                // find this doc in other hits
                for (int other = 0; other < top1.TotalHits; other++)
                {
                    if (top1.ScoreDocs[other].Doc == id)
                    {
                        found = true;
                        float otherScore = top1.ScoreDocs[other].Score;
                        // check if scores match
                        assertEquals(score, otherScore, CheckHits.ExplainToleranceDelta(score, otherScore)); //, "Doc " + id + " scores don't match\n" + CheckHits.TopdocsString(top1, 0, 0) + CheckHits.TopdocsString(top2, 0, 0) + "for query:" + q.ToString());
                    }
                }

                // check if subset
                if (!found)
                {
                    Assert.True(false, "Doc " + id + " not found\n" + CheckHits.TopdocsString(top1, 0, 0) + CheckHits.TopdocsString(top2, 0, 0) + "for query:" + q.ToString());
                }
            }
        }

        [Fact]
        public virtual void TestRewriteCoord1()
        {
            Similarity oldSimilarity = _fixture.IndexSearcher.Similarity;
            try
            {
                _fixture.IndexSearcher.Similarity = new DefaultSimilarityAnonymousInnerClassHelper(this);
                BooleanQuery q1 = new BooleanQuery();
                q1.Add(new TermQuery(new Term("data", "1")), BooleanClause.Occur.SHOULD);
                BooleanQuery q2 = new BooleanQuery();
                q2.Add(new TermQuery(new Term("data", "1")), BooleanClause.Occur.SHOULD);
                q2.MinimumNumberShouldMatch = 1;
                TopDocs top1 = _fixture.IndexSearcher.Search(q1, null, 100);
                TopDocs top2 = _fixture.IndexSearcher.Search(q2, null, 100);
                AssertSubsetOfSameScores(q2, top1, top2);
            }
            finally
            {
                _fixture.IndexSearcher.Similarity = oldSimilarity;
            }
        }

        private class DefaultSimilarityAnonymousInnerClassHelper : DefaultSimilarity
        {
            private readonly TestBooleanMinShouldMatch OuterInstance;

            public DefaultSimilarityAnonymousInnerClassHelper(TestBooleanMinShouldMatch outerInstance)
            {
                this.OuterInstance = outerInstance;
            }

            public override float Coord(int overlap, int maxOverlap)
            {
                return overlap / ((float)maxOverlap + 1);
            }
        }

        [Fact]
        public virtual void TestRewriteNegate()
        {
            Similarity oldSimilarity = _fixture.IndexSearcher.Similarity;
            try
            {
                _fixture.IndexSearcher.Similarity = new DefaultSimilarityAnonymousInnerClassHelper2(this);
                BooleanQuery q1 = new BooleanQuery();
                q1.Add(new TermQuery(new Term("data", "1")), BooleanClause.Occur.SHOULD);
                BooleanQuery q2 = new BooleanQuery();
                q2.Add(new TermQuery(new Term("data", "1")), BooleanClause.Occur.SHOULD);
                q2.Add(new TermQuery(new Term("data", "Z")), BooleanClause.Occur.MUST_NOT);
                TopDocs top1 = _fixture.IndexSearcher.Search(q1, null, 100);
                TopDocs top2 = _fixture.IndexSearcher.Search(q2, null, 100);
                AssertSubsetOfSameScores(q2, top1, top2);
            }
            finally
            {
                _fixture.IndexSearcher.Similarity = oldSimilarity;
            }
        }

        private class DefaultSimilarityAnonymousInnerClassHelper2 : DefaultSimilarity
        {
            private readonly TestBooleanMinShouldMatch OuterInstance;

            public DefaultSimilarityAnonymousInnerClassHelper2(TestBooleanMinShouldMatch outerInstance)
            {
                this.OuterInstance = outerInstance;
            }

            public override float Coord(int overlap, int maxOverlap)
            {
                return overlap / ((float)maxOverlap + 1);
            }
        }

        protected internal virtual void PrintHits(string test, ScoreDoc[] h, IndexSearcher searcher)
        {
            Console.Error.WriteLine("------- " + test + " -------");

            NumberFormatInfo f = new NumberFormatInfo();
            f.NumberDecimalSeparator = ".";

            //DecimalFormat f = new DecimalFormat("0.000000", DecimalFormatSymbols.getInstance(Locale.ROOT));

            for (int i = 0; i < h.Length; i++)
            {
                Document d = searcher.Doc(h[i].Doc);
                decimal score = (decimal)h[i].Score;
                Console.Error.WriteLine("#" + i + ": " + score.ToString(f) + " - " + d.Get("id") + " - " + d.Get("data"));
            }
        }
    }

    public class TestBooleanMinShouldMatchFixture : IDisposable
    {
        private Directory _directory;

        internal IndexReader IndexReader { get; private set; }
        internal IndexSearcher IndexSearcher { get; private set; }

        public TestBooleanMinShouldMatchFixture()
        {
            string[] data = new string[] { "A 1 2 3 4 5 6", "Z       4 5 6", null, "B   2   4 5 6", "Y     3   5 6", null, "C     3     6", "X       4 5 6" };

            _directory = LuceneTestCase.NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(LuceneTestCase.Random(), _directory);

            for (int i = 0; i < data.Length; i++)
            {
                Document doc = new Document();
                doc.Add(LuceneTestCase.NewStringField("id", Convert.ToString(i), Field.Store.YES)); //Field.Keyword("id",String.valueOf(i)));
                doc.Add(LuceneTestCase.NewStringField("all", "all", Field.Store.YES)); //Field.Keyword("all","all"));
                if (null != data[i])
                {
                    doc.Add(LuceneTestCase.NewTextField("data", data[i], Field.Store.YES)); //Field.Text("data",data[i]));
                }
                w.AddDocument(doc);
            }

            IndexReader = w.Reader;
            IndexSearcher = LuceneTestCase.NewSearcher(IndexReader);
            w.Dispose();
            //System.out.println("Set up " + getName());
        }

        public void Dispose()
        {
            IndexSearcher = null;
            IndexReader.Dispose();
            IndexReader = null;
            _directory.Dispose();
            _directory = null;
        }
    }
}