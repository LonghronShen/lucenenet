﻿using System.Collections;
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
using Lucene.Net.Index;
using Lucene.Net.Queries.Function.DocValues;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Lucene.Net.Util.Mutable;

namespace Lucene.Net.Queries.Function.ValueSources
{
    /// <summary>
	/// Obtains double field values from <seealso cref="FieldCache#getDoubles"/> and makes
	/// those values available as other numeric types, casting as needed.
	/// </summary>
	public class DoubleFieldSource : FieldCacheSource
	{

	  protected internal readonly FieldCache_Fields.DoubleParser parser;

	  public DoubleFieldSource(string field) : this(field, null)
	  {
	  }

	  public DoubleFieldSource(string field, FieldCache.DoubleParser parser) : base(field)
	  {
		this.parser = parser;
	  }

        public override string Description
        {
            get { return "double(" + field + ')'; }
        }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public org.apache.lucene.queries.function.FunctionValues GetValues(java.util.Map context, org.apache.lucene.index.AtomicReaderContext readerContext) throws java.io.IOException
	  public override FunctionValues GetValues(IDictionary context, AtomicReaderContext readerContext)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.search.FieldCache.Doubles arr = cache.getDoubles(readerContext.reader(), field, parser, true);
		FieldCache.Doubles arr = cache.getDoubles(readerContext.reader(), field, parser, true);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.util.Bits valid = cache.getDocsWithField(readerContext.reader(), field);
		var valid = cache.getDocsWithField(readerContext.reader(), field);
		return new DoubleDocValuesAnonymousInnerClassHelper(this, this, arr, valid);

	  }

	  private class DoubleDocValuesAnonymousInnerClassHelper : DoubleDocValues
	  {
		  private readonly DoubleFieldSource outerInstance;

		  private FieldCache.Doubles arr;
		  private Bits valid;

		  public DoubleDocValuesAnonymousInnerClassHelper(DoubleFieldSource outerInstance, DoubleFieldSource this, FieldCache.Doubles arr, Bits valid) : base(this)
		  {
			  this.outerInstance = outerInstance;
			  this.arr = arr;
			  this.valid = valid;
		  }

		  public override double DoubleVal(int doc)
		  {
			return arr.get(doc);
		  }

		  public override bool exists(int doc)
		  {
			return arr.get(doc) != 0 || valid.get(doc);
		  }

		  public override ValueFiller ValueFiller
		  {
			  get
			  {
				return new ValueFillerAnonymousInnerClassHelper(this);
			  }
		  }

		  private class ValueFillerAnonymousInnerClassHelper : AbstractValueFiller
		  {
			  private readonly DoubleDocValuesAnonymousInnerClassHelper outerInstance;

			  public ValueFillerAnonymousInnerClassHelper(DoubleDocValuesAnonymousInnerClassHelper outerInstance)
			  {
				  this.outerInstance = outerInstance;
				  mval = new MutableValueDouble();
			  }

			  private readonly MutableValueDouble mval;

			  public override MutableValue Value
			  {
				  get
				  {
					return mval;
				  }
			  }

			  public override void FillValue(int doc)
			  {
				mval.Value = outerInstance.arr.Get(doc);
				mval.Exists = mval.Value != 0 || outerInstance.valid.Get(doc);
			  }
		  }


	  }

	  public override bool Equals(object o)
	  {
		if (o.GetType() != typeof(DoubleFieldSource))
		{
			return false;
		}
		DoubleFieldSource other = (DoubleFieldSource) o;
		return base.Equals(other) && (this.parser == null ? other.parser == null : this.parser.GetType() == other.parser.GetType());
	  }

	  public override int GetHashCode()
	  {
		int h = parser == null ? typeof(double?).GetHashCode() : parser.GetType().GetHashCode();
		h += base.GetHashCode();
		return h;
	  }
	}

}