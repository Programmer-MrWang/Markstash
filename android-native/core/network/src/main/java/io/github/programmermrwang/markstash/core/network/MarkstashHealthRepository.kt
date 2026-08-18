package io.github.programmermrwang.markstash.core.network

import io.github.programmermrwang.markstash.core.model.ApiHealth
import io.github.programmermrwang.markstash.core.model.ApiHealthResult
import io.github.programmermrwang.markstash.core.model.AppLogRepository
import io.github.programmermrwang.markstash.core.model.HealthRepository
import io.github.programmermrwang.markstash.core.model.LogLevel
import java.util.concurrent.TimeUnit
import kotlinx.serialization.json.Json
import okhttp3.OkHttpClient
import retrofit2.Response
import retrofit2.Retrofit
import retrofit2.converter.kotlinx.serialization.asConverterFactory
import retrofit2.http.GET
import okhttp3.MediaType.Companion.toMediaType

class MarkstashHealthRepository(
    private val logs: AppLogRepository,
    private val client: OkHttpClient = OkHttpClient.Builder()
        .connectTimeout(8, TimeUnit.SECONDS)
        .readTimeout(12, TimeUnit.SECONDS)
        .build(),
) : HealthRepository {
    private val json = Json {
        ignoreUnknownKeys = true
        explicitNulls = false
    }

    override suspend fun fetchHealth(baseUrl: String): ApiHealthResult {
        val normalized = EndpointPolicy.normalizeBaseUrl(baseUrl)
        logs.append(LogLevel.Info, "network", "GET ${normalized}api/v1/health")
        return runCatching {
            val service = Retrofit.Builder()
                .baseUrl(normalized)
                .client(client)
                .addConverterFactory(json.asConverterFactory("application/json".toMediaType()))
                .build()
                .create(MarkstashApi::class.java)
            service.health()
        }.fold(
            onSuccess = { response -> response.toResult(normalized) },
            onFailure = { error ->
                val message = error.message ?: error::class.java.simpleName
                logs.append(LogLevel.Error, "network", message)
                ApiHealthResult.Failure(message)
            },
        )
    }

    private fun Response<ApiHealth>.toResult(baseUrl: String): ApiHealthResult {
        val body = body()
        if (isSuccessful && body != null) {
            logs.append(LogLevel.Info, "network", "${code()} ${baseUrl}api/v1/health")
            return ApiHealthResult.Success(body)
        }

        val message = "HTTP ${code()} ${message()}".trim()
        logs.append(LogLevel.Warning, "network", message)
        return ApiHealthResult.Failure(message)
    }
}

private interface MarkstashApi {
    @GET("api/v1/health")
    suspend fun health(): Response<ApiHealth>
}
