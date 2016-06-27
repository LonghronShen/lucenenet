using System;
using System.Diagnostics;
using Lucene.Net.Documents;
using Xunit;

namespace Lucene.Net.Search
{
    using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using Directory = Lucene.Net.Store.Directory;
    using DirectoryReader = Lucene.Net.Index.DirectoryReader;
    using Document = Documents.Document;
    using Field = Field;
    using FieldType = FieldType;
    using FloatField = FloatField;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using IntField = IntField;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

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

    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using MultiFields = Lucene.Net.Index.MultiFields;
    using NumericUtils = Lucene.Net.Util.NumericUtils;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using SlowCompositeReaderWrapper = Lucene.Net.Index.SlowCompositeReaderWrapper;
    using Terms = Lucene.Net.Index.Terms;
    using TermsEnum = Lucene.Net.Index.TermsEnum;
    using TestNumericUtils = Lucene.Net.Util.TestNumericUtils; // NaN arrays
    using TestUtil = Lucene.Net.Util.TestUtil;

    public class TestNumericRangeQuery32 : LuceneTestCase, IClassFixture<TestNumericRangeQuery32Fixture>
    {
        private readonly TestNumericRangeQuery32Fixture _fixture;

        public TestNumericRangeQuery32(TestNumericRangeQuery32Fixture fixture) : base()
        {
            // set the theoretical maximum term count for 8bit (see docs for the number)
            // super.tearDown will restore the default
            BooleanQuery.MaxClauseCount = 3 * 255 * 2 + 255;
        }

        /// <summary>
        /// test for both constant score and boolean query, the other tests only use the constant score mode </summary>
        private void TestRange(int precisionStep)
        {
            string field = "field" + precisionStep;
            int count = 3000;
            int lower = (_fixture.Distance * 3 / 2) + TestNumericRangeQuery32Fixture.StartOffset, upper = lower + count * _fixture.Distance + (_fixture.Distance / 3);
            NumericRangeQuery<int> q = NumericRangeQuery.NewIntRange(field, precisionStep, lower, upper, true, true);
            NumericRangeFilter<int> f = NumericRangeFilter.NewIntRange(field, precisionStep, lower, upper, true, true);
            for (sbyte i = 0; i < 3; i++)
            {
                TopDocs topDocs;
                string type;
                switch (i)
                {
                    case 0:
                        type = " (constant score filter rewrite)";
                        q.SetRewriteMethod(MultiTermQuery.CONSTANT_SCORE_FILTER_REWRITE);
                        topDocs = _fixture.Searcher.Search(q, null, _fixture.NoDocs, Sort.INDEXORDER);
                        break;

                    case 1:
                        type = " (constant score boolean rewrite)";
                        q.SetRewriteMethod(MultiTermQuery.CONSTANT_SCORE_BOOLEAN_QUERY_REWRITE);
                        topDocs = _fixture.Searcher.Search(q, null, _fixture.NoDocs, Sort.INDEXORDER);
                        break;

                    case 2:
                        type = " (filter)";
                        topDocs = _fixture.Searcher.Search(new MatchAllDocsQuery(), f, _fixture.NoDocs, Sort.INDEXORDER);
                        break;

                    default:
                        return;
                }
                ScoreDoc[] sd = topDocs.ScoreDocs;
                Assert.NotNull(sd);
                Assert.Equal(count, sd.Length); //, "Score doc count" + type);
                Document doc = _fixture.Searcher.Doc(sd[0].Doc);
                Assert.Equal(2 * _fixture.Distance + TestNumericRangeQuery32Fixture.StartOffset, (int)doc.GetField(field).NumericValue); //, "First doc" + type);
                doc = _fixture.Searcher.Doc(sd[sd.Length - 1].Doc);
                Assert.Equal((1 + count) * _fixture.Distance + TestNumericRangeQuery32Fixture.StartOffset, (int)doc.GetField(field).NumericValue); //, "Last doc" + type);
            }
        }

        [Fact]
        public virtual void TestRange_8bit()
        {
            TestRange(8);
        }

        [Fact]
        public virtual void TestRange_4bit()
        {
            TestRange(4);
        }

        [Fact]
        public virtual void TestRange_2bit()
        {
            TestRange(2);
        }

        [Fact]
        public virtual void TestInverseRange()
        {
            AtomicReaderContext context = (AtomicReaderContext)SlowCompositeReaderWrapper.Wrap(_fixture.Reader).Context;
            NumericRangeFilter<int> f = NumericRangeFilter.NewIntRange("field8", 8, 1000, -1000, true, true);
            Assert.Null(f.GetDocIdSet(context, (context.AtomicReader).LiveDocs)); //, "A inverse range should return the null instance");
            f = NumericRangeFilter.NewIntRange("field8", 8, int.MaxValue, null, false, false);
            Assert.Null(f.GetDocIdSet(context, (context.AtomicReader).LiveDocs)); //, "A exclusive range starting with Integer.MAX_VALUE should return the null instance");
            f = NumericRangeFilter.NewIntRange("field8", 8, null, int.MinValue, false, false);
            Assert.Null(f.GetDocIdSet(context, (context.AtomicReader).LiveDocs)); //, "A exclusive range ending with Integer.MIN_VALUE should return the null instance");
        }

        [Fact]
        public virtual void TestOneMatchQuery()
        {
            NumericRangeQuery<int> q = NumericRangeQuery.NewIntRange("ascfield8", 8, 1000, 1000, true, true);
            TopDocs topDocs = _fixture.Searcher.Search(q, _fixture.NoDocs);
            ScoreDoc[] sd = topDocs.ScoreDocs;
            Assert.NotNull(sd);
            Assert.Equal(1, sd.Length); //, "Score doc count");
        }

        private void TestLeftOpenRange(int precisionStep)
        {
            string field = "field" + precisionStep;
            int count = 3000;
            int upper = (count - 1) * _fixture.Distance + (_fixture.Distance / 3) + TestNumericRangeQuery32Fixture.StartOffset;
            NumericRangeQuery<int> q = NumericRangeQuery.NewIntRange(field, precisionStep, null, upper, true, true);
            TopDocs topDocs = _fixture.Searcher.Search(q, null, _fixture.NoDocs, Sort.INDEXORDER);
            ScoreDoc[] sd = topDocs.ScoreDocs;
            Assert.NotNull(sd);
            Assert.Equal(count, sd.Length); //, "Score doc count");
            Document doc = _fixture.Searcher.Doc(sd[0].Doc);
            Assert.Equal(TestNumericRangeQuery32Fixture.StartOffset, (int)doc.GetField(field).NumericValue); //, "First doc");
            doc = _fixture.Searcher.Doc(sd[sd.Length - 1].Doc);
            Assert.Equal((count - 1) * _fixture.Distance + TestNumericRangeQuery32Fixture.StartOffset, (int)doc.GetField(field).NumericValue); //, "Last doc");

            q = NumericRangeQuery.NewIntRange(field, precisionStep, null, upper, false, true);
            topDocs = _fixture.Searcher.Search(q, null, _fixture.NoDocs, Sort.INDEXORDER);
            sd = topDocs.ScoreDocs;
            Assert.NotNull(sd);
            Assert.Equal(count, sd.Length); //, "Score doc count");
            doc = _fixture.Searcher.Doc(sd[0].Doc);
            Assert.Equal(TestNumericRangeQuery32Fixture.StartOffset, (int)doc.GetField(field).NumericValue); //, "First doc");
            doc = _fixture.Searcher.Doc(sd[sd.Length - 1].Doc);
            Assert.Equal((count - 1) * _fixture.Distance + TestNumericRangeQuery32Fixture.StartOffset, (int)doc.GetField(field).NumericValue); //, "Last doc");
        }

        [Fact]
        public virtual void TestLeftOpenRange_8bit()
        {
            TestLeftOpenRange(8);
        }

        [Fact]
        public virtual void TestLeftOpenRange_4bit()
        {
            TestLeftOpenRange(4);
        }

        [Fact]
        public virtual void TestLeftOpenRange_2bit()
        {
            TestLeftOpenRange(2);
        }

        private void TestRightOpenRange(int precisionStep)
        {
            string field = "field" + precisionStep;
            int count = 3000;
            int lower = (count - 1) * _fixture.Distance + (_fixture.Distance / 3) + TestNumericRangeQuery32Fixture.StartOffset;
            NumericRangeQuery<int> q = NumericRangeQuery.NewIntRange(field, precisionStep, lower, null, true, true);
            TopDocs topDocs = _fixture.Searcher.Search(q, null, _fixture.NoDocs, Sort.INDEXORDER);
            ScoreDoc[] sd = topDocs.ScoreDocs;
            Assert.NotNull(sd);
            Assert.Equal(_fixture.NoDocs - count, sd.Length); //, "Score doc count");
            Document doc = _fixture.Searcher.Doc(sd[0].Doc);
            Assert.Equal(count * _fixture.Distance + TestNumericRangeQuery32Fixture.StartOffset, (int)doc.GetField(field).NumericValue); //, "First doc");
            doc = _fixture.Searcher.Doc(sd[sd.Length - 1].Doc);
            Assert.Equal((_fixture.NoDocs - 1) * _fixture.Distance + TestNumericRangeQuery32Fixture.StartOffset, (int)doc.GetField(field).NumericValue); //, "Last doc");

            q = NumericRangeQuery.NewIntRange(field, precisionStep, lower, null, true, false);
            topDocs = _fixture.Searcher.Search(q, null, _fixture.NoDocs, Sort.INDEXORDER);
            sd = topDocs.ScoreDocs;
            Assert.NotNull(sd);
            Assert.Equal(_fixture.NoDocs - count, sd.Length); //, "Score doc count");
            doc = _fixture.Searcher.Doc(sd[0].Doc);
            Assert.Equal(count * _fixture.Distance + TestNumericRangeQuery32Fixture.StartOffset, (int)doc.GetField(field).NumericValue); //, "First doc");
            doc = _fixture.Searcher.Doc(sd[sd.Length - 1].Doc);
            Assert.Equal((_fixture.NoDocs - 1) * _fixture.Distance + TestNumericRangeQuery32Fixture.StartOffset, (int)doc.GetField(field).NumericValue); //, "Last doc");
        }

        [Fact]
        public virtual void TestRightOpenRange_8bit()
        {
            TestRightOpenRange(8);
        }

        [Fact]
        public virtual void TestRightOpenRange_4bit()
        {
            TestRightOpenRange(4);
        }

        public virtual void TestRightOpenRange_2bit()
        {
            TestRightOpenRange(2);
        }

        [Fact]
        public virtual void TestInfiniteValues()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));
            Document doc = new Document();
            doc.Add(new FloatField("float", float.NegativeInfinity, Field.Store.NO));
            doc.Add(new IntField("int", int.MinValue, Field.Store.NO));
            writer.AddDocument(doc);

            doc = new Document();
            doc.Add(new FloatField("float", float.PositiveInfinity, Field.Store.NO));
            doc.Add(new IntField("int", int.MaxValue, Field.Store.NO));
            writer.AddDocument(doc);

            doc = new Document();
            doc.Add(new FloatField("float", 0.0f, Field.Store.NO));
            doc.Add(new IntField("int", 0, Field.Store.NO));
            writer.AddDocument(doc);

            foreach (float f in TestNumericUtils.FLOAT_NANs)
            {
                doc = new Document();
                doc.Add(new FloatField("float", f, Field.Store.NO));
                writer.AddDocument(doc);
            }

            writer.Dispose();

            IndexReader r = DirectoryReader.Open(dir);
            IndexSearcher s = NewSearcher(r);

            Query q = NumericRangeQuery.NewIntRange("int", null, null, true, true);
            TopDocs topDocs = s.Search(q, 10);
            Assert.Equal(3, topDocs.ScoreDocs.Length); //, "Score doc count");

            q = NumericRangeQuery.NewIntRange("int", null, null, false, false);
            topDocs = s.Search(q, 10);
            Assert.Equal(3, topDocs.ScoreDocs.Length); //, "Score doc count");

            q = NumericRangeQuery.NewIntRange("int", int.MinValue, int.MaxValue, true, true);
            topDocs = s.Search(q, 10);
            Assert.Equal(3, topDocs.ScoreDocs.Length); //, "Score doc count");

            q = NumericRangeQuery.NewIntRange("int", int.MinValue, int.MaxValue, false, false);
            topDocs = s.Search(q, 10);
            Assert.Equal(1, topDocs.ScoreDocs.Length); //, "Score doc count");

            q = NumericRangeQuery.NewFloatRange("float", null, null, true, true);
            topDocs = s.Search(q, 10);
            Assert.Equal(3, topDocs.ScoreDocs.Length); //, "Score doc count");

            q = NumericRangeQuery.NewFloatRange("float", null, null, false, false);
            topDocs = s.Search(q, 10);
            Assert.Equal(3, topDocs.ScoreDocs.Length); //, "Score doc count");

            q = NumericRangeQuery.NewFloatRange("float", float.NegativeInfinity, float.PositiveInfinity, true, true);
            topDocs = s.Search(q, 10);
            Assert.Equal(3, topDocs.ScoreDocs.Length); //, "Score doc count");

            q = NumericRangeQuery.NewFloatRange("float", float.NegativeInfinity, float.PositiveInfinity, false, false);
            topDocs = s.Search(q, 10);
            Assert.Equal(1, topDocs.ScoreDocs.Length); //, "Score doc count");

            q = NumericRangeQuery.NewFloatRange("float", float.NaN, float.NaN, true, true);
            topDocs = s.Search(q, 10);
            Assert.Equal(TestNumericUtils.FLOAT_NANs.Length, topDocs.ScoreDocs.Length); //, "Score doc count");

            r.Dispose();
            dir.Dispose();
        }

        private void TestRandomTrieAndClassicRangeQuery(int precisionStep)
        {
            string field = "field" + precisionStep;
            int totalTermCountT = 0, totalTermCountC = 0, termCountT, termCountC;
            int num = TestUtil.NextInt(Random(), 10, 20);
            for (int i = 0; i < num; i++)
            {
                int lower = (int)(Random().NextDouble() * _fixture.NoDocs * _fixture.Distance) + TestNumericRangeQuery32Fixture.StartOffset;
                int upper = (int)(Random().NextDouble() * _fixture.NoDocs * _fixture.Distance) + TestNumericRangeQuery32Fixture.StartOffset;
                if (lower > upper)
                {
                    int a = lower;
                    lower = upper;
                    upper = a;
                }
                BytesRef lowerBytes = new BytesRef(NumericUtils.BUF_SIZE_INT), upperBytes = new BytesRef(NumericUtils.BUF_SIZE_INT);
                NumericUtils.IntToPrefixCodedBytes(lower, 0, lowerBytes);
                NumericUtils.IntToPrefixCodedBytes(upper, 0, upperBytes);

                // test inclusive range
                NumericRangeQuery<int> tq = NumericRangeQuery.NewIntRange(field, precisionStep, lower, upper, true, true);
                TermRangeQuery cq = new TermRangeQuery(field, lowerBytes, upperBytes, true, true);
                TopDocs tTopDocs = _fixture.Searcher.Search(tq, 1);
                TopDocs cTopDocs = _fixture.Searcher.Search(cq, 1);
                Assert.Equal(cTopDocs.TotalHits, tTopDocs.TotalHits); //, "Returned count for NumericRangeQuery and TermRangeQuery must be equal");
                totalTermCountT += termCountT = CountTerms(tq);
                totalTermCountC += termCountC = CountTerms(cq);
                CheckTermCounts(precisionStep, termCountT, termCountC);
                // test exclusive range
                tq = NumericRangeQuery.NewIntRange(field, precisionStep, lower, upper, false, false);
                cq = new TermRangeQuery(field, lowerBytes, upperBytes, false, false);
                tTopDocs = _fixture.Searcher.Search(tq, 1);
                cTopDocs = _fixture.Searcher.Search(cq, 1);
                Assert.Equal(cTopDocs.TotalHits, tTopDocs.TotalHits); //, "Returned count for NumericRangeQuery and TermRangeQuery must be equal");
                totalTermCountT += termCountT = CountTerms(tq);
                totalTermCountC += termCountC = CountTerms(cq);
                CheckTermCounts(precisionStep, termCountT, termCountC);
                // test left exclusive range
                tq = NumericRangeQuery.NewIntRange(field, precisionStep, lower, upper, false, true);
                cq = new TermRangeQuery(field, lowerBytes, upperBytes, false, true);
                tTopDocs = _fixture.Searcher.Search(tq, 1);
                cTopDocs = _fixture.Searcher.Search(cq, 1);
                Assert.Equal(cTopDocs.TotalHits, tTopDocs.TotalHits); //, "Returned count for NumericRangeQuery and TermRangeQuery must be equal");
                totalTermCountT += termCountT = CountTerms(tq);
                totalTermCountC += termCountC = CountTerms(cq);
                CheckTermCounts(precisionStep, termCountT, termCountC);
                // test right exclusive range
                tq = NumericRangeQuery.NewIntRange(field, precisionStep, lower, upper, true, false);
                cq = new TermRangeQuery(field, lowerBytes, upperBytes, true, false);
                tTopDocs = _fixture.Searcher.Search(tq, 1);
                cTopDocs = _fixture.Searcher.Search(cq, 1);
                Assert.Equal(cTopDocs.TotalHits, tTopDocs.TotalHits); //, "Returned count for NumericRangeQuery and TermRangeQuery must be equal");
                totalTermCountT += termCountT = CountTerms(tq);
                totalTermCountC += termCountC = CountTerms(cq);
                CheckTermCounts(precisionStep, termCountT, termCountC);
            }

            CheckTermCounts(precisionStep, totalTermCountT, totalTermCountC);
            if (VERBOSE && precisionStep != int.MaxValue)
            {
                Console.WriteLine("Average number of terms during random search on '" + field + "':");
                Console.WriteLine(" Numeric query: " + (((double)totalTermCountT) / (num * 4)));
                Console.WriteLine(" Classical query: " + (((double)totalTermCountC) / (num * 4)));
            }
        }

        [Fact]
        public virtual void TestEmptyEnums()
        {
            int count = 3000;
            int lower = (_fixture.Distance * 3 / 2) + TestNumericRangeQuery32Fixture.StartOffset, upper = lower + count * _fixture.Distance + (_fixture.Distance / 3);
            // test empty enum
            Debug.Assert(lower < upper);
            Assert.True(0 < CountTerms(NumericRangeQuery.NewIntRange("field4", 4, lower, upper, true, true)));
            Assert.Equal(0, CountTerms(NumericRangeQuery.NewIntRange("field4", 4, upper, lower, true, true)));
            // test empty enum outside of bounds
            lower = _fixture.Distance * _fixture.NoDocs + TestNumericRangeQuery32Fixture.StartOffset;
            upper = 2 * lower;
            Debug.Assert(lower < upper);
            Assert.Equal(0, CountTerms(NumericRangeQuery.NewIntRange("field4", 4, lower, upper, true, true)));
        }

        private int CountTerms(MultiTermQuery q)
        {
            Terms terms = MultiFields.GetTerms(_fixture.Reader, q.Field);
            if (terms == null)
            {
                return 0;
            }
            TermsEnum termEnum = q.GetTermsEnum(terms);
            Assert.NotNull(termEnum);
            int count = 0;
            BytesRef cur, last = null;
            while ((cur = termEnum.Next()) != null)
            {
                count++;
                if (last != null)
                {
                    Assert.True(last.CompareTo(cur) < 0);
                }
                last = BytesRef.DeepCopyOf(cur);
            }
            // LUCENE-3314: the results after next() already returned null are undefined,
            // Assert.Null(termEnum.Next());
            return count;
        }

        private void CheckTermCounts(int precisionStep, int termCountT, int termCountC)
        {
            if (precisionStep == int.MaxValue)
            {
                Assert.Equal(termCountC, termCountT); //, "Number of terms should be equal for unlimited precStep");
            }
            else
            {
                Assert.True(termCountT <= termCountC, "Number of terms for NRQ should be <= compared to classical TRQ");
            }
        }

        [Fact]
        public virtual void TestRandomTrieAndClassicRangeQuery_8bit()
        {
            TestRandomTrieAndClassicRangeQuery(8);
        }

        [Fact]
        public virtual void TestRandomTrieAndClassicRangeQuery_4bit()
        {
            TestRandomTrieAndClassicRangeQuery(4);
        }

        [Fact]
        public virtual void TestRandomTrieAndClassicRangeQuery_2bit()
        {
            TestRandomTrieAndClassicRangeQuery(2);
        }

        [Fact]
        public virtual void TestRandomTrieAndClassicRangeQuery_NoTrie()
        {
            TestRandomTrieAndClassicRangeQuery(int.MaxValue);
        }

        private void TestRangeSplit(int precisionStep)
        {
            string field = "ascfield" + precisionStep;
            // 10 random tests
            int num = TestUtil.NextInt(Random(), 10, 20);
            for (int i = 0; i < num; i++)
            {
                int lower = (int)(Random().NextDouble() * _fixture.NoDocs - _fixture.NoDocs / 2);
                int upper = (int)(Random().NextDouble() * _fixture.NoDocs - _fixture.NoDocs / 2);
                if (lower > upper)
                {
                    int a = lower;
                    lower = upper;
                    upper = a;
                }
                // test inclusive range
                Query tq = NumericRangeQuery.NewIntRange(field, precisionStep, lower, upper, true, true);
                TopDocs tTopDocs = _fixture.Searcher.Search(tq, 1);
                Assert.Equal(upper - lower + 1, tTopDocs.TotalHits); //, "Returned count of range query must be equal to inclusive range length");
                // test exclusive range
                tq = NumericRangeQuery.NewIntRange(field, precisionStep, lower, upper, false, false);
                tTopDocs = _fixture.Searcher.Search(tq, 1);
                Assert.Equal(Math.Max(upper - lower - 1, 0), tTopDocs.TotalHits); //, "Returned count of range query must be equal to exclusive range length");
                // test left exclusive range
                tq = NumericRangeQuery.NewIntRange(field, precisionStep, lower, upper, false, true);
                tTopDocs = _fixture.Searcher.Search(tq, 1);
                Assert.Equal(upper - lower, tTopDocs.TotalHits); //, "Returned count of range query must be equal to half exclusive range length");
                // test right exclusive range
                tq = NumericRangeQuery.NewIntRange(field, precisionStep, lower, upper, true, false);
                tTopDocs = _fixture.Searcher.Search(tq, 1);
                Assert.Equal(upper - lower, tTopDocs.TotalHits); //, "Returned count of range query must be equal to half exclusive range length");
            }
        }

        [Fact]
        public virtual void TestRangeSplit_8bit()
        {
            TestRangeSplit(8);
        }

        [Fact]
        public virtual void TestRangeSplit_4bit()
        {
            TestRangeSplit(4);
        }

        [Fact]
        public virtual void TestRangeSplit_2bit()
        {
            TestRangeSplit(2);
        }

        /// <summary>
        /// we fake a float test using int2float conversion of NumericUtils </summary>
        private void TestFloatRange(int precisionStep)
        {
            string field = "ascfield" + precisionStep;
            const int lower = -1000, upper = +2000;

            Query tq = NumericRangeQuery.NewFloatRange(field, precisionStep, NumericUtils.SortableIntToFloat(lower), NumericUtils.SortableIntToFloat(upper), true, true);
            TopDocs tTopDocs = _fixture.Searcher.Search(tq, 1);
            Assert.Equal(upper - lower + 1, tTopDocs.TotalHits); //, "Returned count of range query must be equal to inclusive range length");

            Filter tf = NumericRangeFilter.NewFloatRange(field, precisionStep, NumericUtils.SortableIntToFloat(lower), NumericUtils.SortableIntToFloat(upper), true, true);
            tTopDocs = _fixture.Searcher.Search(new MatchAllDocsQuery(), tf, 1);
            Assert.Equal(upper - lower + 1, tTopDocs.TotalHits); //, "Returned count of range filter must be equal to inclusive range length");
        }

        [Fact]
        public virtual void TestFloatRange_8bit()
        {
            TestFloatRange(8);
        }

        [Fact]
        public virtual void TestFloatRange_4bit()
        {
            TestFloatRange(4);
        }

        [Fact]
        public virtual void TestFloatRange_2bit()
        {
            TestFloatRange(2);
        }

        private void TestSorting(int precisionStep)
        {
            string field = "field" + precisionStep;
            // 10 random tests, the index order is ascending,
            // so using a reverse sort field should retun descending documents
            int num = TestUtil.NextInt(Random(), 10, 20);
            for (int i = 0; i < num; i++)
            {
                int lower = (int)(Random().NextDouble() * _fixture.NoDocs * _fixture.Distance) + TestNumericRangeQuery32Fixture.StartOffset;
                int upper = (int)(Random().NextDouble() * _fixture.NoDocs * _fixture.Distance) + TestNumericRangeQuery32Fixture.StartOffset;
                if (lower > upper)
                {
                    int a = lower;
                    lower = upper;
                    upper = a;
                }
                Query tq = NumericRangeQuery.NewIntRange(field, precisionStep, lower, upper, true, true);
                TopDocs topDocs = _fixture.Searcher.Search(tq, null, _fixture.NoDocs, new Sort(new SortField(field, SortField.Type_e.INT, true)));
                if (topDocs.TotalHits == 0)
                {
                    continue;
                }
                ScoreDoc[] sd = topDocs.ScoreDocs;
                Assert.NotNull(sd);
                int last = (int)_fixture.Searcher.Doc(sd[0].Doc).GetField(field).NumericValue;
                for (int j = 1; j < sd.Length; j++)
                {
                    int act = (int)_fixture.Searcher.Doc(sd[j].Doc).GetField(field).NumericValue;
                    Assert.True(last > act, "Docs should be sorted backwards");
                    last = act;
                }
            }
        }

        [Fact]
        public virtual void TestSorting_8bit()
        {
            TestSorting(8);
        }

        [Fact]
        public virtual void TestSorting_4bit()
        {
            TestSorting(4);
        }

        [Fact]
        public virtual void TestSorting_2bit()
        {
            TestSorting(2);
        }

        [Fact]
        public virtual void TestEqualsAndHash()
        {
            QueryUtils.CheckHashEquals(NumericRangeQuery.NewIntRange("test1", 4, 10, 20, true, true));
            QueryUtils.CheckHashEquals(NumericRangeQuery.NewIntRange("test2", 4, 10, 20, false, true));
            QueryUtils.CheckHashEquals(NumericRangeQuery.NewIntRange("test3", 4, 10, 20, true, false));
            QueryUtils.CheckHashEquals(NumericRangeQuery.NewIntRange("test4", 4, 10, 20, false, false));
            QueryUtils.CheckHashEquals(NumericRangeQuery.NewIntRange("test5", 4, 10, null, true, true));
            QueryUtils.CheckHashEquals(NumericRangeQuery.NewIntRange("test6", 4, null, 20, true, true));
            QueryUtils.CheckHashEquals(NumericRangeQuery.NewIntRange("test7", 4, null, null, true, true));
            QueryUtils.CheckEqual(NumericRangeQuery.NewIntRange("test8", 4, 10, 20, true, true), NumericRangeQuery.NewIntRange("test8", 4, 10, 20, true, true));
            QueryUtils.CheckUnequal(NumericRangeQuery.NewIntRange("test9", 4, 10, 20, true, true), NumericRangeQuery.NewIntRange("test9", 8, 10, 20, true, true));
            QueryUtils.CheckUnequal(NumericRangeQuery.NewIntRange("test10a", 4, 10, 20, true, true), NumericRangeQuery.NewIntRange("test10b", 4, 10, 20, true, true));
            QueryUtils.CheckUnequal(NumericRangeQuery.NewIntRange("test11", 4, 10, 20, true, true), NumericRangeQuery.NewIntRange("test11", 4, 20, 10, true, true));
            QueryUtils.CheckUnequal(NumericRangeQuery.NewIntRange("test12", 4, 10, 20, true, true), NumericRangeQuery.NewIntRange("test12", 4, 10, 20, false, true));
            QueryUtils.CheckUnequal(NumericRangeQuery.NewIntRange("test13", 4, 10, 20, true, true), NumericRangeQuery.NewFloatRange("test13", 4, 10f, 20f, true, true));
            // the following produces a hash collision, because Long and Integer have the same hashcode, so only test equality:
            Query q1 = NumericRangeQuery.NewIntRange("test14", 4, 10, 20, true, true);
            Query q2 = NumericRangeQuery.NewLongRange("test14", 4, 10L, 20L, true, true);
            Assert.False(q1.Equals(q2));
            Assert.False(q2.Equals(q1));
        }
    }

    public class TestNumericRangeQuery32Fixture : IDisposable
    {
        // shift the starting of the values to the left, to also have negative values:
        internal static readonly int StartOffset = -1 << 15;

        // distance of entries
        internal int Distance { get; private set; }
        // number of docs to generate for testing
        internal int NoDocs { get; private set; }

        internal Directory Directory { get; private set; }
        internal IndexReader Reader { get; private set; }
        internal IndexSearcher Searcher { get; private set; }

        public TestNumericRangeQuery32Fixture()
        {
            var random = LuceneTestCase.Random();
            NoDocs = LuceneTestCase.AtLeast(4096);
            Distance = (1 << 30) / NoDocs;
            Directory = LuceneTestCase.NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(random, Directory, LuceneTestCase.NewIndexWriterConfig(LuceneTestCase.TEST_VERSION_CURRENT, new MockAnalyzer(random)).SetMaxBufferedDocs(TestUtil.NextInt(random, 100, 1000)).SetMergePolicy(LuceneTestCase.NewLogMergePolicy()));

            FieldType storedInt = new FieldType(IntField.TYPE_NOT_STORED);
            storedInt.Stored = true;
            storedInt.Freeze();

            FieldType storedInt8 = new FieldType(storedInt);
            storedInt8.NumericPrecisionStep = 8;

            FieldType storedInt4 = new FieldType(storedInt);
            storedInt4.NumericPrecisionStep = 4;

            FieldType storedInt2 = new FieldType(storedInt);
            storedInt2.NumericPrecisionStep = 2;

            FieldType storedIntNone = new FieldType(storedInt);
            storedIntNone.NumericPrecisionStep = int.MaxValue;

            FieldType unstoredInt = IntField.TYPE_NOT_STORED;

            FieldType unstoredInt8 = new FieldType(unstoredInt);
            unstoredInt8.NumericPrecisionStep = 8;

            FieldType unstoredInt4 = new FieldType(unstoredInt);
            unstoredInt4.NumericPrecisionStep = 4;

            FieldType unstoredInt2 = new FieldType(unstoredInt);
            unstoredInt2.NumericPrecisionStep = 2;

            IntField field8 = new IntField("field8", 0, storedInt8), field4 = new IntField("field4", 0, storedInt4), field2 = new IntField("field2", 0, storedInt2), fieldNoTrie = new IntField("field" + int.MaxValue, 0, storedIntNone), ascfield8 = new IntField("ascfield8", 0, unstoredInt8), ascfield4 = new IntField("ascfield4", 0, unstoredInt4), ascfield2 = new IntField("ascfield2", 0, unstoredInt2);

            Document doc = new Document();
            // add fields, that have a distance to test general functionality
            doc.Add(field8);
            doc.Add(field4);
            doc.Add(field2);
            doc.Add(fieldNoTrie);
            // add ascending fields with a distance of 1, beginning at -NoDocs/2 to test the correct splitting of range and inclusive/exclusive
            doc.Add(ascfield8);
            doc.Add(ascfield4);
            doc.Add(ascfield2);

            // Add a series of NoDocs docs with increasing int values
            for (int l = 0; l < NoDocs; l++)
            {
                int val = Distance * l + StartOffset;
                field8.IntValue = val;
                field4.IntValue = val;
                field2.IntValue = val;
                fieldNoTrie.IntValue = val;

                val = l - (NoDocs / 2);
                ascfield8.IntValue = val;
                ascfield4.IntValue = val;
                ascfield2.IntValue = val;
                writer.AddDocument(doc);
            }

            Reader = writer.Reader;
            Searcher = LuceneTestCase.NewSearcher(Reader);
            writer.Dispose();
        }

        public void Dispose()
        {
            Searcher = null;
            Reader.Dispose();
            Reader = null;
            Directory.Dispose();
            Directory = null;
        }
    }
}