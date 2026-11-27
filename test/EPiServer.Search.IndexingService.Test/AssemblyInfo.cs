using Xunit;

// Tests in this assembly read and modify the static state in IndexingServiceSettings
// (NamedIndexElements, NamedIndexDirectories, ...), which are plain Dictionary instances.
// Running test classes in parallel causes intermittent failures such as
// "Collection was modified; enumeration operation may not execute.", so run them sequentially.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
