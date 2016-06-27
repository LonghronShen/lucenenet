using System;
using System.Collections.Generic;
using Lucene.Net.Documents;
using Xunit;

namespace Lucene.Net.Search.Spans
{
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using Field = Field;
    using IndexReader = Lucene.Net.Index.IndexReader;
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
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using Term = Lucene.Net.Index.Term;
    using TFIDFSimilarity = Lucene.Net.Search.Similarities.TFIDFSimilarity;

    public class TestFieldMaskingSpanQuery : LuceneTestCase, IClassFixture<TestFieldMaskingSpanQueryFixture>
    {
        private TestFieldMaskingSpanQueryFixture _fixture;

        public TestFieldMaskingSpanQuery(TestFieldMaskingSpanQueryFixture fixture)
        {
            _fixture = fixture;
        }

        protected internal virtual void Check(SpanQuery q, int[] docs)
        {
            CheckHits.CheckHitCollector(Random(), q, null, _fixture.Searcher, docs);
        }

        [Fact]
        public virtual void TestRewrite0()
        {
            SpanQuery q = new FieldMaskingSpanQuery(new SpanTermQuery(new Term("last", "sally")), "first");
            q.Boost = 8.7654321f;
            SpanQuery qr = (SpanQuery)_fixture.Searcher.Rewrite(q);

            QueryUtils.CheckEqual(q, qr);

            HashSet<Term> terms = new HashSet<Term>();
            qr.ExtractTerms(terms);
            Assert.Equal(1, terms.Count);
        }

        [Fact]
        public virtual void TestRewrite1()
        {
            // mask an anon SpanQuery class that rewrites to something else.
            SpanQuery q = new FieldMaskingSpanQuery(new SpanTermQueryAnonymousInnerClassHelper(this, new Term("last", "sally")), "first");

            SpanQuery qr = (SpanQuery)_fixture.Searcher.Rewrite(q);

            QueryUtils.CheckUnequal(q, qr);

            HashSet<Term> terms = new HashSet<Term>();
            qr.ExtractTerms(terms);
            Assert.Equal(2, terms.Count);
        }

        private class SpanTermQueryAnonymousInnerClassHelper : SpanTermQuery
        {
            private readonly TestFieldMaskingSpanQuery OuterInstance;

            public SpanTermQueryAnonymousInnerClassHelper(TestFieldMaskingSpanQuery outerInstance, Term term)
                : base(term)
            {
                this.OuterInstance = outerInstance;
            }

            public override Query Rewrite(IndexReader reader)
            {
                return new SpanOrQuery(new SpanTermQuery(new Term("first", "sally")), new SpanTermQuery(new Term("first", "james")));
            }
        }

        [Fact]
        public virtual void TestRewrite2()
        {
            SpanQuery q1 = new SpanTermQuery(new Term("last", "smith"));
            SpanQuery q2 = new SpanTermQuery(new Term("last", "jones"));
            SpanQuery q = new SpanNearQuery(new SpanQuery[] { q1, new FieldMaskingSpanQuery(q2, "last") }, 1, true);
            Query qr = _fixture.Searcher.Rewrite(q);

            QueryUtils.CheckEqual(q, qr);

            HashSet<Term> set = new HashSet<Term>();
            qr.ExtractTerms(set);
            Assert.Equal(2, set.Count);
        }

        [Fact]
        public virtual void TestEquality1()
        {
            SpanQuery q1 = new FieldMaskingSpanQuery(new SpanTermQuery(new Term("last", "sally")), "first");
            SpanQuery q2 = new FieldMaskingSpanQuery(new SpanTermQuery(new Term("last", "sally")), "first");
            SpanQuery q3 = new FieldMaskingSpanQuery(new SpanTermQuery(new Term("last", "sally")), "XXXXX");
            SpanQuery q4 = new FieldMaskingSpanQuery(new SpanTermQuery(new Term("last", "XXXXX")), "first");
            SpanQuery q5 = new FieldMaskingSpanQuery(new SpanTermQuery(new Term("xXXX", "sally")), "first");
            QueryUtils.CheckEqual(q1, q2);
            QueryUtils.CheckUnequal(q1, q3);
            QueryUtils.CheckUnequal(q1, q4);
            QueryUtils.CheckUnequal(q1, q5);

            SpanQuery qA = new FieldMaskingSpanQuery(new SpanTermQuery(new Term("last", "sally")), "first");
            qA.Boost = 9f;
            SpanQuery qB = new FieldMaskingSpanQuery(new SpanTermQuery(new Term("last", "sally")), "first");
            QueryUtils.CheckUnequal(qA, qB);
            qB.Boost = 9f;
            QueryUtils.CheckEqual(qA, qB);
        }

        [Fact]
        public virtual void TestNoop0()
        {
            SpanQuery q1 = new SpanTermQuery(new Term("last", "sally"));
            SpanQuery q = new FieldMaskingSpanQuery(q1, "first");
            Check(q, new int[] { }); // :EMPTY:
        }

        [Fact]
        public virtual void TestNoop1()
        {
            SpanQuery q1 = new SpanTermQuery(new Term("last", "smith"));
            SpanQuery q2 = new SpanTermQuery(new Term("last", "jones"));
            SpanQuery q = new SpanNearQuery(new SpanQuery[] { q1, new FieldMaskingSpanQuery(q2, "last") }, 0, true);
            Check(q, new int[] { 1, 2 });
            q = new SpanNearQuery(new SpanQuery[] { new FieldMaskingSpanQuery(q1, "last"), new FieldMaskingSpanQuery(q2, "last") }, 0, true);
            Check(q, new int[] { 1, 2 });
        }

        [Fact]
        public virtual void TestSimple1()
        {
            SpanQuery q1 = new SpanTermQuery(new Term("first", "james"));
            SpanQuery q2 = new SpanTermQuery(new Term("last", "jones"));
            SpanQuery q = new SpanNearQuery(new SpanQuery[] { q1, new FieldMaskingSpanQuery(q2, "first") }, -1, false);
            Check(q, new int[] { 0, 2 });
            q = new SpanNearQuery(new SpanQuery[] { new FieldMaskingSpanQuery(q2, "first"), q1 }, -1, false);
            Check(q, new int[] { 0, 2 });
            q = new SpanNearQuery(new SpanQuery[] { q2, new FieldMaskingSpanQuery(q1, "last") }, -1, false);
            Check(q, new int[] { 0, 2 });
            q = new SpanNearQuery(new SpanQuery[] { new FieldMaskingSpanQuery(q1, "last"), q2 }, -1, false);
            Check(q, new int[] { 0, 2 });
        }

        [Fact]
        public virtual void TestSimple2()
        {
            AssumeTrue("Broken scoring: LUCENE-3723", _fixture.Searcher.Similarity is TFIDFSimilarity);
            SpanQuery q1 = new SpanTermQuery(new Term("gender", "female"));
            SpanQuery q2 = new SpanTermQuery(new Term("last", "smith"));
            SpanQuery q = new SpanNearQuery(new SpanQuery[] { q1, new FieldMaskingSpanQuery(q2, "gender") }, -1, false);
            Check(q, new int[] { 2, 4 });
            q = new SpanNearQuery(new SpanQuery[] { new FieldMaskingSpanQuery(q1, "id"), new FieldMaskingSpanQuery(q2, "id") }, -1, false);
            Check(q, new int[] { 2, 4 });
        }

        [Fact]
        public virtual void TestSpans0()
        {
            SpanQuery q1 = new SpanTermQuery(new Term("gender", "female"));
            SpanQuery q2 = new SpanTermQuery(new Term("first", "james"));
            SpanQuery q = new SpanOrQuery(q1, new FieldMaskingSpanQuery(q2, "gender"));
            Check(q, new int[] { 0, 1, 2, 3, 4 });

            Spans span = MultiSpansWrapper.Wrap(_fixture.Searcher.TopReaderContext, q);

            Assert.Equal(true, span.Next());
            Assert.Equal(s(0, 0, 1), s(span));

            Assert.Equal(true, span.Next());
            Assert.Equal(s(1, 0, 1), s(span));

            Assert.Equal(true, span.Next());
            Assert.Equal(s(1, 1, 2), s(span));

            Assert.Equal(true, span.Next());
            Assert.Equal(s(2, 0, 1), s(span));

            Assert.Equal(true, span.Next());
            Assert.Equal(s(2, 1, 2), s(span));

            Assert.Equal(true, span.Next());
            Assert.Equal(s(2, 2, 3), s(span));

            Assert.Equal(true, span.Next());
            Assert.Equal(s(3, 0, 1), s(span));

            Assert.Equal(true, span.Next());
            Assert.Equal(s(4, 0, 1), s(span));

            Assert.Equal(true, span.Next());
            Assert.Equal(s(4, 1, 2), s(span));

            Assert.Equal(false, span.Next());
        }

        [Fact]
        public virtual void TestSpans1()
        {
            SpanQuery q1 = new SpanTermQuery(new Term("first", "sally"));
            SpanQuery q2 = new SpanTermQuery(new Term("first", "james"));
            SpanQuery qA = new SpanOrQuery(q1, q2);
            SpanQuery qB = new FieldMaskingSpanQuery(qA, "id");

            Check(qA, new int[] { 0, 1, 2, 4 });
            Check(qB, new int[] { 0, 1, 2, 4 });

            Spans spanA = MultiSpansWrapper.Wrap(_fixture.Searcher.TopReaderContext, qA);
            Spans spanB = MultiSpansWrapper.Wrap(_fixture.Searcher.TopReaderContext, qB);

            while (spanA.Next())
            {
                Assert.True(spanB.Next(), "spanB not still going");
                Assert.Equal(s(spanA), s(spanB)); //, "spanA not equal spanB");
            }
            Assert.True(!(spanB.Next()), "spanB still going even tough spanA is done");
        }

        [Fact]
        public virtual void TestSpans2()
        {
            AssumeTrue("Broken scoring: LUCENE-3723", _fixture.Searcher.Similarity is TFIDFSimilarity);
            SpanQuery qA1 = new SpanTermQuery(new Term("gender", "female"));
            SpanQuery qA2 = new SpanTermQuery(new Term("first", "james"));
            SpanQuery qA = new SpanOrQuery(qA1, new FieldMaskingSpanQuery(qA2, "gender"));
            SpanQuery qB = new SpanTermQuery(new Term("last", "jones"));
            SpanQuery q = new SpanNearQuery(new SpanQuery[] { new FieldMaskingSpanQuery(qA, "id"), new FieldMaskingSpanQuery(qB, "id") }, -1, false);
            Check(q, new int[] { 0, 1, 2, 3 });

            Spans span = MultiSpansWrapper.Wrap(_fixture.Searcher.TopReaderContext, q);

            Assert.Equal(true, span.Next());
            Assert.Equal(s(0, 0, 1), s(span));

            Assert.Equal(true, span.Next());
            Assert.Equal(s(1, 1, 2), s(span));

            Assert.Equal(true, span.Next());
            Assert.Equal(s(2, 0, 1), s(span));

            Assert.Equal(true, span.Next());
            Assert.Equal(s(2, 2, 3), s(span));

            Assert.Equal(true, span.Next());
            Assert.Equal(s(3, 0, 1), s(span));

            Assert.Equal(false, span.Next());
        }

        public virtual string s(Spans span)
        {
            return s(span.Doc(), span.Start(), span.End());
        }

        public virtual string s(int doc, int start, int end)
        {
            return "s(" + doc + "," + start + "," + end + ")";
        }
    }

    public class TestFieldMaskingSpanQueryFixture : IDisposable
    {
        internal Document Doc(Field[] fields)
        {
            Document doc = new Document();
            for (int i = 0; i < fields.Length; i++)
            {
                doc.Add(fields[i]);
            }
            return doc;
        }

        internal Field GetField(string name, string value)
        {
            return LuceneTestCase.NewTextField(name, value, Field.Store.NO);
        }

        internal IndexSearcher Searcher { get; private set; }
        internal Directory Directory { get; private set; }
        internal IndexReader Reader { get; private set; }

        public TestFieldMaskingSpanQueryFixture()
        {
            Directory = LuceneTestCase.NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(LuceneTestCase.Random(), Directory, LuceneTestCase.NewIndexWriterConfig(LuceneTestCase.TEST_VERSION_CURRENT, new MockAnalyzer(LuceneTestCase.Random())).SetMergePolicy(LuceneTestCase.NewLogMergePolicy()));

            writer.AddDocument(Doc(new Field[] { GetField("id", "0"), GetField("gender", "male"), GetField("first", "james"), GetField("last", "jones") }));

            writer.AddDocument(Doc(new Field[] { GetField("id", "1"), GetField("gender", "male"), GetField("first", "james"), GetField("last", "smith"), GetField("gender", "female"), GetField("first", "sally"), GetField("last", "jones") }));

            writer.AddDocument(Doc(new Field[] { GetField("id", "2"), GetField("gender", "female"), GetField("first", "greta"), GetField("last", "jones"), GetField("gender", "female"), GetField("first", "sally"), GetField("last", "smith"), GetField("gender", "male"), GetField("first", "james"), GetField("last", "jones") }));

            writer.AddDocument(Doc(new Field[] { GetField("id", "3"), GetField("gender", "female"), GetField("first", "lisa"), GetField("last", "jones"), GetField("gender", "male"), GetField("first", "bob"), GetField("last", "costas") }));

            writer.AddDocument(Doc(new Field[] { GetField("id", "4"), GetField("gender", "female"), GetField("first", "sally"), GetField("last", "smith"), GetField("gender", "female"), GetField("first", "linda"), GetField("last", "dixit"), GetField("gender", "male"), GetField("first", "bubba"), GetField("last", "jones") }));
            Reader = writer.Reader;
            writer.Dispose();
            Searcher = LuceneTestCase.NewSearcher(Reader);
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