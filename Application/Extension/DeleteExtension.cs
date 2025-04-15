using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Application.Extension
{
    public static class DeleteExtensions
    {
        // Soft delete for any entity with IsDeleted property
        public static async Task SoftDeleteAsync<T>(this DbContext context, T entity, CancellationToken cancellationToken = default) where T : class
        {
            // Use reflection to make this work with any entity that has IsDeleted property
            PropertyInfo isDeletedProperty = typeof(T).GetProperty("IsDeleted");

            if (isDeletedProperty != null && isDeletedProperty.PropertyType == typeof(bool))
            {
                isDeletedProperty.SetValue(entity, true);
                context.Update(entity);
                await context.SaveChangesAsync(cancellationToken);
                return;
            }

            // Special case handling for specific entities
            if (entity is User user)
            {
                user.IsDeleted = true;
                context.Update(user);
                await context.SaveChangesAsync(cancellationToken);
                return;
            }

            // If no IsDeleted property found, fall back to hard delete
            await context.HardDeleteAsync(entity, cancellationToken);
        }

        // Undelete - restore a soft-deleted entity
        public static async Task<bool> UndeleteAsync<T>(this DbContext context, T entity, CancellationToken cancellationToken = default) where T : class
        {
            PropertyInfo isDeletedProperty = typeof(T).GetProperty("IsDeleted");

            if (isDeletedProperty != null && isDeletedProperty.PropertyType == typeof(bool))
            {
                isDeletedProperty.SetValue(entity, false);
                context.Update(entity);
                await context.SaveChangesAsync(cancellationToken);
                return true;
            }

            if (entity is User user)
            {
                user.IsDeleted = false;
                context.Update(user);
                await context.SaveChangesAsync(cancellationToken);
                return true;
            }

            return false; // Cannot undelete an entity without IsDeleted property
        }

        // Hard delete - removes from database
        public static async Task HardDeleteAsync<T>(this DbContext context, T entity, CancellationToken cancellationToken = default) where T : class
        {
            context.Remove(entity);
            await context.SaveChangesAsync(cancellationToken);
        }

        // Hard delete - removes from database by id
        public static async Task HardDeleteByIdAsync<T>(this DbContext context, int id, CancellationToken cancellationToken = default) where T : class
        {
            var entity = await context.Set<T>().FindAsync(new object[] { id }, cancellationToken);
            if (entity != null)
            {
                context.Remove(entity);
                await context.SaveChangesAsync(cancellationToken);
            }
        }

        // Soft delete - marks as deleted by id
        public static async Task SoftDeleteByIdAsync<T>(this DbContext context, int id, CancellationToken cancellationToken = default) where T : class
        {
            var entity = await context.Set<T>().FindAsync(new object[] { id }, cancellationToken);
            if (entity != null)
            {
                await context.SoftDeleteAsync(entity, cancellationToken);
            }
        }

        // Get all entities including soft-deleted ones
        public static IQueryable<T> IncludeDeleted<T>(this IQueryable<T> query) where T : class
        {
            return query.IgnoreQueryFilters();
        }

        // Check if entity is deleted
        public static bool IsDeleted<T>(this T entity) where T : class
        {
            PropertyInfo isDeletedProperty = typeof(T).GetProperty("IsDeleted");

            if (isDeletedProperty != null && isDeletedProperty.PropertyType == typeof(bool))
            {
                return (bool)isDeletedProperty.GetValue(entity);
            }

            if (entity is User user)
            {
                return user.IsDeleted;
            }

            return false; // Entities without IsDeleted property are considered not deleted
        }
    }
}
