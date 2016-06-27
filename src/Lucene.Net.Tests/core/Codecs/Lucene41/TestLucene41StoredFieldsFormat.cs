namespace Lucene.Net.Codecs.Lucene41
{
    using Tests.Codecs;
    using Xunit;
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

    using BaseStoredFieldsFormatTestCase = Lucene.Net.Index.BaseStoredFieldsFormatTestCase;

    public class TestLucene41StoredFieldsFormat : BaseStoredFieldsFormatTestCase, IClassFixture<OldFormatCodecFixture>
    {
        private readonly OldFormatCodecFixture _fixture;

        public TestLucene41StoredFieldsFormat(OldFormatCodecFixture fixture)
        {
            _fixture = fixture;
        }

        protected override Codec Codec
        {
            get
            {
                return new Lucene41RWCodec();
            }
        }
    }
}