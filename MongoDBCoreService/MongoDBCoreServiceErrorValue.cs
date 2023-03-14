//-----------------------------------------------------------------------
// <copyright file="MongoDBCoreServiceErrorValue.cs" company="Beckhoff Automation GmbH & Co. KG">
//     Copyright (c) Beckhoff Automation GmbH & Co. KG. All Rights Reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace MongoDBCoreService
{
    // Contains extension specific error values.
    public static class MongoDBCoreServiceErrorValue
    {
        public static readonly uint Success = 0;
        public static readonly uint Fail = 1;

        public static readonly uint UpdateDocumentFail = 10;
        public static readonly uint CreateDocumentFail = 11;
        public static readonly uint CreateCollectionFail = 12;
        public static readonly uint DataWrongTypeOrEmpty = 13;
        public static readonly uint RemoveDocumentFail = 14;
        public static readonly uint SetAggregationPipelineFail = 15;
        public static readonly uint GetAggregationResultFail = 16;
    }
}