﻿#region Copyright
// 
// DotNetNuke® - https://www.dnnsoftware.com
// Copyright (c) 2002-2018
// by DotNetNuke Corporation
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and 
// to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial portions 
// of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED 
// TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.
#endregion
#region Usings

using System;
using System.Collections.Generic;

#endregion

namespace DotNetNuke.Web.InternalServices.Views.Search
{
    /// <summary>
    /// BasicView grouped by DocumentTypeName
    /// </summary>
    public class GroupedBasicView
    {
        /// <summary>
        /// Type of Search Document
        /// </summary>
        public string DocumentTypeName { get; set; }

        /// <summary>
        /// Results of the Search
        /// </summary>   
        public List<BasicView> Results { get; set; }

        #region constructor

        public GroupedBasicView()
        {}

        public GroupedBasicView(BasicView basic)
        {
            DocumentTypeName = basic.DocumentTypeName;
            Results = new List<BasicView>
            {
                new BasicView
                {
                    Title = basic.Title,
                    Snippet = basic.Snippet,
                    Description = basic.Description,
                    DocumentUrl = basic.DocumentUrl,
                    Attributes = basic.Attributes
                }
            };
        }

        #endregion
    }
}
