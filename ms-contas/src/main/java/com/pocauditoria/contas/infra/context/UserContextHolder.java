package com.pocauditoria.contas.infra.context;

public final class UserContextHolder {

    private static final ThreadLocal<String> USER_ID = new ThreadLocal<>();
    private static final ThreadLocal<String> CORRELATION_ID = new ThreadLocal<>();

    private UserContextHolder() {
    }

    public static void setCurrentUserId(String userId) {
        USER_ID.set(userId);
    }

    public static String getCurrentUserId() {
        return USER_ID.get();
    }

    public static void setCorrelationId(String correlationId) {
        CORRELATION_ID.set(correlationId);
    }

    public static String getCorrelationId() {
        return CORRELATION_ID.get();
    }

    public static void clear() {
        USER_ID.remove();
        CORRELATION_ID.remove();
    }
}
