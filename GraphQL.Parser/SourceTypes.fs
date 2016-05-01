namespace GraphQL.Parser
open System

/// The position in the source file that a syntactic element appeared.
type SourcePosition =
    {
        Index : int64
        Line : int64
        Column : int64
    }

type ParsingException(msg, pos : SourcePosition) =
    inherit Exception(msg)
    member this.Position = pos

/// The span of (start, end) positions in the source file
/// that a syntactic element occupies.
type SourceInfo =
    {
        StartPosition : SourcePosition
        EndPosition : SourcePosition
    }
    member this.ShowInSource(source : string) =
        // TODO: nicely format, point at the location with ^^^ or something
        source.Substring
            ( int this.StartPosition.Index
            , int (this.EndPosition.Index - this.StartPosition.Index)
            )

/// `'a` with the positions in source that it spanned.
type WithSource<'a> =
    {
        /// The position in source of the syntactic element
        Source : SourceInfo
        /// The syntactic element
        Value : 'a
    }

type SourceException(msg : string, pos : SourceInfo) =
    inherit Exception(msg)
    member this.SourceInfo = pos

type ValidationException(msg : string) =
    inherit Exception(msg)
