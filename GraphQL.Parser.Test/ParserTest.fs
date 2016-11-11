//MIT License
//
//Copyright (c) 2016 Robert Peele
//
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in all
//copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.

namespace GraphQL.Parser.Test
open GraphQL.Parser
open GraphQL.Parser.ParserAST
open NUnit.Framework

// Tests that the parser succeeds on examples taken from the draft GraphQL spec.

[<TestFixture>]
type ParserTest() =
    let good source =
        let doc = GraphQLParserDocument.Parse(source)
        if doc.AST.Definitions.Count <= 0 then
            failwith "No definitions in document!"
    [<Test>]
    member __.TestMutation() =
        good @"
mutation {
  likeStory(storyID: 12345) {
    story {
      likeCount
    }
  }
}"
    [<Test>]
    member __.TestShorthand() =
        good @"
{
  field
}"
    [<Test>]
    member __.TestShorthand3Fields() =
        good @"
{
  id
  firstName
  lastName
}"
    [<Test>]
    member __.TestNestingShorthand() =
        good @"
{
  me {
    id
    firstName
    lastName
    birthday {
      month
      day
    }
    friends {
      name
    }
  }
}"

    [<Test>]
    member __.TestMultiWithComments() =
        good @"
# `me` could represent the currently logged in viewer.
{
  me {
    name
  }
}

# `user` represents one of many users in a graph of data, referred to by a
# unique identifier.
{
  user(id: 4) {
    name
  }
}"

    [<Test>]
    member __.TestArguments() =
        good @"
{
  user(id: 4) {
    id
    name
    profilePic(size: 100)
  }
}"
    [<Test>]
    member __.TestMultiArguments() =
        good @"
{
  user(id: 4) {
    id
    name
    profilePic(width: 100, height: 50)
  }
}"
    [<Test>]
    member __.TestStringArguments() =
        good @"
{
  user(id: ""abc"") {
    id
  }
}"
    [<Test>]
    member __.TestEscapedStringArguments() =
        good @"
{
  user(id: ""'a\""b\""c'"") {
    id
  }
}"
    [<Test>]
    member __.TestFieldAlias() =
        good @"
{
  user(id: 4) {
    id
    name
    smallPic: profilePic(size: 64)
    bigPic: profilePic(size: 1024)
  }
}"

    [<Test>]
    member __.TestFragments() =
        good @"
query withFragments {
  user(id: 4) {
    friends(first: 10) {
      ...friendFields
    }
    mutualFriends(first: 10) {
      ...friendFields
    }
  }
}

fragment friendFields on User {
  id
  name
  profilePic(size: 50)
}
"
    [<Test>]
    member __.TestNestedFragments() =
        good @"
query withNestedFragments {
  user(id: 4) {
    friends(first: 10) {
      ...friendFields
    }
    mutualFriends(first: 10) {
      ...friendFields
    }
  }
}

fragment friendFields on User {
  id
  name
  ...standardProfilePic
}

fragment standardProfilePic on User {
  profilePic(size: 50)
}"

    [<Test>]
    member __.TestFragmentTyping() =
        good @"
query FragmentTyping {
  profiles(handles: [""zuck"", ""cocacola""]) {
    handle
    ...userFragment
    ...pageFragment
  }
}

fragment userFragment on User {
  friends {
    count
  }
}

fragment pageFragment on Page {
  likers {
    count
  }
}
"
    [<Test>]
    member __.TestInlineFragmentTyping() =
        good @"
query inlineFragmentTyping {
  profiles(handles: [""zuck"", ""cocacola""]) {
    handle
    ... on User {
      friends {
        count
      }
    }
    ... on Page {
      likers {
        count
      }
    }
  }
}
"
    [<Test>]
    member __.TestInlineFragmentNoTypeDirective() =
        good @"
query inlineFragmentNoType($expandedInfo: Boolean) {
  user(handle: ""zuck"") {
    id
    name
    ... @include(if: $expandedInfo) {
      firstName
      lastName
      birthday
    }
  }
}
"
    [<Test>]
    member __.TestVariables() =
        good @"
query getZuckProfile($devicePicSize: Int) {
  user(id: 4) {
    id
    name
    profilePic(size: $devicePicSize)
  }
}
"