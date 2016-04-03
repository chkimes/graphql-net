namespace GraphQL.Parser

/// The position in the source file that a syntactic element appeared.
type SourcePosition =
    {
        Index : int64
        Line : int64
        Column : int64
    }

/// The span of (start, end) positions in the source file
/// that a syntactic element occupies.
type SourceInfo =
    {
        StartPosition : SourcePosition
        EndPosition : SourcePosition
    }

/// `'a` with the positions in source that it spanned.
type WithSource<'a> =
    {
        /// The position in source of the syntactic element
        Source : SourceInfo
        /// The syntactic element
        Value : 'a
    }
