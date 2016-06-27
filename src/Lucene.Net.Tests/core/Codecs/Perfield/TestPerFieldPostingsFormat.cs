using System;
using System.Collections.Generic;
using Xunit;

namespace Lucene.Net.Codecs.Perfield
{
    using Index;


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

    using BasePostingsFormatTestCase = Lucene.Net.Index.BasePostingsFormatTestCase;
    using RandomCodec = Lucene.Net.Index.RandomCodec;

    /// <summary>
    /// Basic tests of PerFieldPostingsFormat
    /// </summary>
    public class TestPerFieldPostingsFormat : BasePostingsFormatTestCase
    {
        public TestPerFieldPostingsFormat(BasePostingsFormatTestCaseFixture fixture) : base(fixture)
        {
        }

        protected override Codec Codec
        {
            get
            {
                return new RandomCodec(new Random(Random().Next()), new HashSet<string>());
            }
        }

        [Fact]
        public override void TestMergeStability()
        {
            //LUCENE TO-DO
            AssumeTrue("The MockRandom PF randomizes content on the fly, so we can't check it", false);
        }
    }
}