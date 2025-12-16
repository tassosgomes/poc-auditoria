package com.pocauditoria.contas.api.filter;

import com.pocauditoria.contas.infra.context.UserContextHolder;
import jakarta.servlet.FilterChain;
import jakarta.servlet.ServletException;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import java.io.IOException;
import java.nio.charset.StandardCharsets;
import java.util.Base64;
import java.util.Map;
import java.util.UUID;
import org.slf4j.MDC;
import org.springframework.stereotype.Component;
import org.springframework.web.filter.OncePerRequestFilter;

@Component
public class SimpleAuthFilter extends OncePerRequestFilter {

    private static final Map<String, String> VALID_CREDENTIALS = Map.of(
            "admin", "admin123",
            "user", "user123"
    );

    @Override
    protected void doFilterInternal(
            HttpServletRequest request,
            HttpServletResponse response,
            FilterChain filterChain
    ) throws ServletException, IOException {

        String path = request.getRequestURI();
        if (path.contains("/swagger")
                || path.contains("/v3/api-docs")
                || path.contains("/health")) {
            filterChain.doFilter(request, response);
            return;
        }

        try {
            String correlationId = request.getHeader("X-Correlation-Id");
            if (correlationId == null || correlationId.isBlank()) {
                correlationId = UUID.randomUUID().toString();
            }
            UserContextHolder.setCorrelationId(correlationId);
            response.setHeader("X-Correlation-Id", correlationId);
            MDC.put("correlationId", correlationId);

            String authHeader = request.getHeader("Authorization");
            if (authHeader == null || !authHeader.startsWith("Basic ")) {
                response.setStatus(HttpServletResponse.SC_UNAUTHORIZED);
                return;
            }

            String decoded = new String(
                    Base64.getDecoder().decode(authHeader.substring(6)),
                    StandardCharsets.UTF_8
            );
            String[] parts = decoded.split(":", 2);

            if (parts.length != 2 || !VALID_CREDENTIALS.getOrDefault(parts[0], "").equals(parts[1])) {
                response.setStatus(HttpServletResponse.SC_UNAUTHORIZED);
                return;
            }

            UserContextHolder.setCurrentUserId(parts[0]);
            MDC.put("userId", parts[0]);

            filterChain.doFilter(request, response);
        } finally {
            UserContextHolder.clear();
            MDC.clear();
        }
    }
}
