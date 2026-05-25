namespace RedPandaFlow.Api.Hubs
{
    public record PresenceUser(Guid UserId, string Username, string? AvatarUrl);

    public interface IBoardPresenceTracker
    {
        IReadOnlyList<PresenceUser> Add(Guid boardId, string connectionId, PresenceUser user);
        IReadOnlyList<PresenceUser> Remove(Guid boardId, string connectionId);
        IEnumerable<(Guid BoardId, IReadOnlyList<PresenceUser> Snapshot)> RemoveConnection(string connectionId);
        IReadOnlyList<PresenceUser> Snapshot(Guid boardId);
    }

    public class BoardPresenceTracker : IBoardPresenceTracker
    {
        private readonly object _lock = new();
        private readonly Dictionary<Guid, Dictionary<string, PresenceUser>> _byBoard = new();
        private readonly Dictionary<string, HashSet<Guid>> _byConnection = new();

        public IReadOnlyList<PresenceUser> Add(Guid boardId, string connectionId, PresenceUser user)
        {
            lock (_lock)
            {
                if (!_byBoard.TryGetValue(boardId, out var conns))
                {
                    conns = new Dictionary<string, PresenceUser>();
                    _byBoard[boardId] = conns;
                }
                conns[connectionId] = user;

                if (!_byConnection.TryGetValue(connectionId, out var boards))
                {
                    boards = new HashSet<Guid>();
                    _byConnection[connectionId] = boards;
                }
                boards.Add(boardId);

                return DistinctByUser(conns.Values);
            }
        }

        public IReadOnlyList<PresenceUser> Remove(Guid boardId, string connectionId)
        {
            lock (_lock)
            {
                if (!_byBoard.TryGetValue(boardId, out var conns))
                {
                    return Array.Empty<PresenceUser>();
                }

                conns.Remove(connectionId);
                if (conns.Count == 0)
                {
                    _byBoard.Remove(boardId);
                }

                if (_byConnection.TryGetValue(connectionId, out var boards))
                {
                    boards.Remove(boardId);
                    if (boards.Count == 0)
                    {
                        _byConnection.Remove(connectionId);
                    }
                }

                return DistinctByUser(conns.Values);
            }
        }

        public IEnumerable<(Guid BoardId, IReadOnlyList<PresenceUser> Snapshot)> RemoveConnection(string connectionId)
        {
            lock (_lock)
            {
                if (!_byConnection.TryGetValue(connectionId, out var boards))
                {
                    return Enumerable.Empty<(Guid, IReadOnlyList<PresenceUser>)>();
                }

                var results = new List<(Guid, IReadOnlyList<PresenceUser>)>();
                foreach (var boardId in boards.ToList())
                {
                    if (_byBoard.TryGetValue(boardId, out var conns))
                    {
                        conns.Remove(connectionId);
                        if (conns.Count == 0)
                        {
                            _byBoard.Remove(boardId);
                        }
                        results.Add((boardId, DistinctByUser(conns.Values)));
                    }
                }
                _byConnection.Remove(connectionId);
                return results;
            }
        }

        public IReadOnlyList<PresenceUser> Snapshot(Guid boardId)
        {
            lock (_lock)
            {
                if (_byBoard.TryGetValue(boardId, out var conns))
                {
                    return DistinctByUser(conns.Values);
                }
                return Array.Empty<PresenceUser>();
            }
        }

        private static IReadOnlyList<PresenceUser> DistinctByUser(IEnumerable<PresenceUser> users)
        {
            return users
                .GroupBy(u => u.UserId)
                .Select(g => g.First())
                .ToList();
        }
    }
}
