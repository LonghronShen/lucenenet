﻿using Lucene.Net.Documents;
using Lucene.Net.QueryParsers.Flexible.Core.Config;
using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Core.Processors;
using Lucene.Net.QueryParsers.Flexible.Standard.Config;
using Lucene.Net.QueryParsers.Flexible.Standard.Nodes;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Processors
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

    /// <summary>
    /// This processors process <see cref="TermRangeQueryNode"/>s. It reads the lower and
    /// upper bounds value from the <see cref="TermRangeQueryNode"/> object and try
    /// to parse their values using a <paramref name="dateFormat"/>. If the values cannot be
    /// parsed to a date value, it will only create the <see cref="TermRangeQueryNode"/>
    /// using the non-parsed values.
    /// <para/>
    /// If a <see cref="ConfigurationKeys.LOCALE"/> is defined in the
    /// <see cref="QueryConfigHandler"/> it will be used to parse the date, otherwise
    /// <see cref="CultureInfo.CurrentCulture"/> will be used.
    /// <para/>
    /// If a <see cref="ConfigurationKeys.DATE_RESOLUTION"/> is defined and the
    /// <see cref="DateTools.Resolution"/> is not <c>null</c> it will also be used to parse the
    /// date value.
    /// </summary>
    /// <seealso cref="ConfigurationKeys.DATE_RESOLUTION"/>
    /// <seealso cref="ConfigurationKeys.LOCALE"/>
    /// <seealso cref="TermRangeQueryNode"/>
    public class TermRangeQueryNodeProcessor : QueryNodeProcessorImpl
    {
        public TermRangeQueryNodeProcessor()
        {
            // empty constructor
        }

        protected override IQueryNode PostProcessNode(IQueryNode node)
        {
            if (node is TermRangeQueryNode)
            {
                TermRangeQueryNode termRangeNode = (TermRangeQueryNode)node;
                FieldQueryNode upper = (FieldQueryNode)termRangeNode.UpperBound;
                FieldQueryNode lower = (FieldQueryNode)termRangeNode.LowerBound;

                // LUCENENET TODO: Add a NOT_SET value so we have a logical default?
                DateTools.Resolution dateRes = DateTools.Resolution.MINUTE/* = null*/;
                bool inclusive = false;
                CultureInfo locale = GetQueryConfigHandler().Get(ConfigurationKeys.LOCALE);

                if (locale == null)
                {
                    locale = CultureInfo.CurrentCulture; //Locale.getDefault();
                }

                TimeZoneInfo timeZone = GetQueryConfigHandler().Get(ConfigurationKeys.TIMEZONE);

                if (timeZone == null)
                {
                    timeZone = TimeZoneInfo.Local; //TimeZone.getDefault();
                }

                string field = termRangeNode.Field;
                string fieldStr = null;

                if (field != null)
                {
                    fieldStr = field.ToString();
                }

                FieldConfig fieldConfig = GetQueryConfigHandler()
                    .GetFieldConfig(fieldStr);

                if (fieldConfig != null)
                {
                    dateRes = fieldConfig.Get(ConfigurationKeys.DATE_RESOLUTION);
                }

                if (termRangeNode.IsUpperInclusive)
                {
                    inclusive = true;
                }

                string part1 = lower.GetTextAsString();
                string part2 = upper.GetTextAsString();

                try
                {
                    //DateFormat df = DateFormat.GetDateInstance(DateFormat.SHORT, locale);
                    //df.setLenient(true);

                    if (part1.Length > 0)
                    {
                        //DateTime d1 = df.parse(part1);
                        DateTime d1 = DateTime.Parse(part1, locale);
                        part1 = DateTools.DateToString(d1, dateRes);
                        lower.Text = new StringCharSequenceWrapper(part1);
                    }

                    if (part2.Length > 0)
                    {
                        //DateTime d2 = df.parse(part2);
                        DateTime d2 = DateTime.Parse(part2, locale);
                        if (inclusive)
                        {
                            // The user can only specify the date, not the time, so make sure
                            // the time is set to the latest possible time of that date to
                            // really
                            // include all documents:
                            //Calendar cal = Calendar.getInstance(timeZone, locale);
                            //cal.setTime(d2);
                            //cal.set(Calendar.HOUR_OF_DAY, 23);
                            //cal.set(Calendar.MINUTE, 59);
                            //cal.set(Calendar.SECOND, 59);
                            //cal.set(Calendar.MILLISECOND, 999);
                            //d2 = cal.getTime();

                            var cal = locale.Calendar;
                            d2 = cal.AddHours(d2, 23);
                            d2 = cal.AddMinutes(d2, 59);
                            d2 = cal.AddSeconds(d2, 59);
                            d2 = cal.AddMilliseconds(d2, 999);
                        }

                        part2 = DateTools.DateToString(d2, dateRes);
                        upper.Text = new StringCharSequenceWrapper(part2);
                    }
                }
                catch (Exception e)
                {
                    // do nothing
                }
            }

            return node;
        }

        protected override IQueryNode PreProcessNode(IQueryNode node)
        {
            return node;
        }

        protected override IList<IQueryNode> SetChildrenOrder(IList<IQueryNode> children)
        {
            return children;
        }
    }
}