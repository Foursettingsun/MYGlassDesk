using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using GlassDesk.Core.Models;
using GlassDesk.Core.Storage;

namespace GlassDesk.Core.Repair;

public static class LauncherPagingRepair
{
    private static List<int>? Sizes(object owner) => (List<int>?)owner.GetType().GetProperty("FlatPageSizes")!.GetValue(owner);
    private static void SetSizes(object owner, List<int>? sizes) => owner.GetType().GetProperty("FlatPageSizes")!.SetValue(owner, sizes == null ? null : new List<int>(sizes));

    private static List<string> Ordered(LauncherLayout layout)
    {
        var all = LauncherLayoutRules.AppIds(layout);
        var valid = new HashSet<string>(all, StringComparer.Ordinal);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<string>();
        foreach (string id in layout.FlatOrder ?? new List<string>()) if (valid.Contains(id) && seen.Add(id)) result.Add(id);
        foreach (string id in all) if (seen.Add(id)) result.Add(id);
        return result;
    }

    private static LauncherGroup? FindGroup(LauncherLayout layout, string? id)
    {
        if (id == null) return null;
        foreach (var group in layout.FilterGroups ?? new List<LauncherGroup>()) if (group.Id == id) return group;
        return null;
    }

    private static List<string> Members(LauncherLayout layout, string? groupId)
    {
        var ordered = Ordered(layout);
        if (groupId == null) return ordered;
        var allowed = new HashSet<string>(FindGroup(layout, groupId)?.ItemIds ?? new List<string>(), StringComparer.Ordinal);
        ordered.RemoveAll(id => !allowed.Contains(id));
        return ordered;
    }

    public static LauncherLayout Project(LauncherLayout source, string? groupId, int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);
        var ids = Members(source, groupId);
        object? scope = groupId == null ? source : FindGroup(source, groupId);
        List<int>? boundaries = scope == null ? null : Sizes(scope);
        var result = new LauncherLayout { SchemaVersion = 2 };
        int offset = 0;
        if (boundaries != null)
        {
            foreach (int requested in boundaries)
            {
                if (offset >= ids.Count && requested != 0) break;
                if (requested < 0) continue;
                int remaining = Math.Min(requested, ids.Count - offset);
                if (remaining == 0) AddPage(result, ids, ref offset, 0);
                while (remaining > 0)
                {
                    int count = Math.Min(capacity, remaining);
                    AddPage(result, ids, ref offset, count);
                    remaining -= count;
                }
            }
        }
        while (offset < ids.Count) AddPage(result, ids, ref offset, Math.Min(capacity, ids.Count - offset));
        while (result.Pages.Count > 0 && result.Pages[result.Pages.Count - 1].Slots.Count == 0) result.Pages.RemoveAt(result.Pages.Count - 1);
        return result;
    }

    private static void AddPage(LauncherLayout layout, List<string> ids, ref int offset, int count)
    {
        var page = new LauncherPage { Id = "flat-" + layout.Pages.Count };
        for (int index = 0; index < count; index++) page.Slots.Add(new LauncherSlot { ItemId = ids[offset++] });
        layout.Pages.Add(page);
    }

    public static LauncherLayout PlaceAtSlot(LauncherLayout projection, string id, int finalIndex, int capacity, bool exchange)
    {
        // The page editor preserves partially filled pages and supports a new trailing page.
        return LauncherLayoutEditor.PlaceAtSlot(projection, id, finalIndex, capacity, exchange);
    }

    public static LauncherLayout ApplyProjection(LauncherLayout source, LauncherLayout projection, string? groupId, int capacity)
    {
        var beforeIds = Members(source, groupId);
        var before = new HashSet<string>(beforeIds, StringComparer.Ordinal);
        var afterIds = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var pageSizes = new List<int>();
        foreach (var page in projection.Pages)
        {
            if (page.Slots.Count > capacity) throw new InvalidOperationException("页面图标数量超出容量，请重试。");
            pageSizes.Add(page.Slots.Count);
            foreach (var slot in page.Slots)
            {
                if (slot.Kind != LauncherSlotKind.App || slot.ItemId == null || !before.Contains(slot.ItemId) || !seen.Add(slot.ItemId))
                    throw new InvalidOperationException("平铺整理包含无效的图标，请重试。");
                afterIds.Add(slot.ItemId);
            }
        }
        var result = LauncherLayoutRules.DeepClone(source);
        foreach (string id in beforeIds) if (!seen.Contains(id)) result = LauncherLayoutEditor.RemoveShortcut(result, id, capacity);
        var ordered = Ordered(result);
        var replacements = new Queue<string>(afterIds);
        result.FlatOrder = new List<string>();
        foreach (string id in ordered) result.FlatOrder.Add(before.Contains(id) ? replacements.Dequeue() : id);
        LauncherFilterRules.Normalize(result);
        foreach (var other in result.FilterGroups)
        {
            if (other.Id == groupId) continue;
            var original = FindGroup(source, other.Id);
            if (original != null) SetSizes(other, AdjustSizes(Sizes(original), Members(source, other.Id), Members(result, other.Id)));
        }
        if (groupId == null) SetSizes(result, pageSizes);
        else
        {
            var group = FindGroup(result, groupId) ?? throw new InvalidOperationException("分组已不存在，请重试。");
            SetSizes(group, pageSizes);
            SetSizes(result, AdjustSizes(Sizes(source), Ordered(source), Ordered(result)));
        }
        return result;
    }

    public static LauncherLayout CopyPaging(LauncherLayout clone, LauncherLayout source)
    {
        SetSizes(clone, Sizes(source));
        foreach (var group in clone.FilterGroups)
        {
            var original = FindGroup(source, group.Id);
            if (original != null) SetSizes(group, Sizes(original));
        }
        return clone;
    }

    public static LauncherLayout CopyAfterReconcile(LauncherLayout clone, LauncherLayout source, int capacity)
    {
        CopyPaging(clone, source);
        SetSizes(clone, AdjustSizes(Sizes(source), Ordered(source), Ordered(clone)));
        foreach (var group in clone.FilterGroups)
        {
            var original = FindGroup(source, group.Id);
            if (original != null) SetSizes(group, AdjustSizes(Sizes(original), Members(source, group.Id), Members(clone, group.Id)));
        }
        return clone;
    }

    private static List<int>? AdjustSizes(List<int>? sizes, List<string> before, List<string> after)
    {
        if (sizes == null) return null;
        var allowed = new HashSet<string>(after, StringComparer.Ordinal);
        var result = new List<int>();
        int offset = 0;
        foreach (int size in sizes)
        {
            int count = 0;
            int end = Math.Min(before.Count, offset + Math.Max(0, Math.Min(size, before.Count)));
            while (offset < end) if (allowed.Contains(before[offset++])) count++;
            result.Add(count);
        }
        return result;
    }

    public static void NormalizePaging(LauncherLayout layout)
    {
        var before = new List<string>(layout.FlatOrder ?? new List<string>());
        if (before.Count == 0) return;
        var after = Ordered(layout);
        SetSizes(layout, AdjustSizes(Sizes(layout), before, after));
        foreach (var group in layout.FilterGroups ?? new List<LauncherGroup>())
        {
            var allowed = new HashSet<string>(group.ItemIds ?? new List<string>(), StringComparer.Ordinal);
            var beforeMembers = new List<string>();
            var afterMembers = new List<string>();
            foreach (var id in before) if (allowed.Contains(id)) beforeMembers.Add(id);
            foreach (var id in after) if (allowed.Contains(id)) afterMembers.Add(id);
            SetSizes(group, AdjustSizes(Sizes(group), beforeMembers, afterMembers));
        }
    }

    public static bool PagingEquivalent(LauncherLayout left, LauncherLayout right)
    {
        if (!EqualSizes(Sizes(left), Sizes(right))) return false;
        foreach (var group in left.FilterGroups ?? new List<LauncherGroup>())
        {
            var match = FindGroup(right, group.Id);
            if (match != null && !EqualSizes(Sizes(group), Sizes(match))) return false;
        }
        return true;
    }

    private static bool EqualSizes(List<int>? left, List<int>? right)
    {
        if (left == null || right == null) return left == right;
        if (left.Count != right.Count) return false;
        for (int index = 0; index < left.Count; index++) if (left[index] != right[index]) return false;
        return true;
    }

    public static void CopyStorePaging(LayoutStore store, LauncherLayout source) { SetSizes(store.Launcher, Sizes(source)); }

    public static JsonTypeInfo ResolveLayoutInfo(Type type, JsonSerializerOptions options)
    {
        return new DefaultJsonTypeInfoResolver().GetTypeInfo(type, options)!;
    }

    public static JsonTypeInfo? ResolveExtraInfo(Type type, JsonSerializerOptions options)
    {
        return type == typeof(List<int>) ? ResolveLayoutInfo(type, options) : null;
    }
}
