using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using InventorySystem.Application.DTOs;
using Microsoft.Data.SqlClient;
using InventorySystem.Application.Interfaces;

namespace InventorySystem.Infrastructure.Persistence
{
    public class FifoStockRepository(IConfiguration configuration,ILogger<FifoStockRepository> logger ):IFifoStockRepository
    { 
        private readonly string _connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        public async Task<List<BatchConsumedDetail>> ConsumeStockFifoAsync(
            int productId,
            int quantityToConsume,
            string reason,
            string consumedBy,
            CancellationToken ct = default)
        {
            var consumedDetails = new List<BatchConsumedDetail>();

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(ct);

            logger.LogInformation(
                "Executing sp_ConsumeStockFIFO — ProductId={ProductId}, Quantity={Qty}",
                productId, quantityToConsume);

            await using var command = new SqlCommand("sp_ConsumeStockFIFO", connection)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = 30
            };

            command.Parameters.AddWithValue("@ProductId", productId);
            command.Parameters.AddWithValue("@QuantityToConsume", quantityToConsume);
            command.Parameters.AddWithValue("@Reason", reason);
            command.Parameters.AddWithValue("@ConsumedBy", consumedBy);

            var errorMessageParam = new SqlParameter("@ErrorMessage", SqlDbType.NVarChar, 500)
            {
                Direction = ParameterDirection.Output
            };
            command.Parameters.Add(errorMessageParam);

            await using var reader = await command.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                consumedDetails.Add(new BatchConsumedDetail(
                    BatchId: reader.GetInt32(reader.GetOrdinal("BatchId")),
                    BatchNumber: reader.GetString(reader.GetOrdinal("BatchNumber")),
                    QuantityConsumed: reader.GetInt32(reader.GetOrdinal("QuantityConsumed"))
                ));
            }

            var errorMessage = errorMessageParam.Value?.ToString();
            if (!string.IsNullOrEmpty(errorMessage))
            {
                logger.LogWarning("sp_ConsumeStockFIFO returned error: {Error}", errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            logger.LogInformation(
                "FIFO consumption succeeded — {BatchCount} batch(es) affected", consumedDetails.Count);

            return consumedDetails;
        }

    }
}
