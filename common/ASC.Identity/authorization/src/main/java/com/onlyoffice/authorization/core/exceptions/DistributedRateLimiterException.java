package com.onlyoffice.authorization.core.exceptions;

/**
 * 
 */
public class DistributedRateLimiterException extends RuntimeException {
    public DistributedRateLimiterException(String message) {
        super(message);
    }
}
