/**
 *
 */
package com.onlyoffice.authorization.api.web.server.controllers;

import com.onlyoffice.authorization.api.configuration.ApplicationConfiguration;
import com.onlyoffice.authorization.api.core.entities.Action;
import com.onlyoffice.authorization.api.core.exceptions.EntityCreationException;
import com.onlyoffice.authorization.api.core.usecases.service.authorization.AuthorizationCleanupUsecases;
import com.onlyoffice.authorization.api.core.usecases.service.client.ClientCleanupUsecases;
import com.onlyoffice.authorization.api.core.usecases.service.client.ClientCreationUsecases;
import com.onlyoffice.authorization.api.core.usecases.service.client.ClientMutationUsecases;
import com.onlyoffice.authorization.api.core.usecases.service.client.ClientRetrieveUsecases;
import com.onlyoffice.authorization.api.core.usecases.service.consent.ConsentCleanupUsecases;
import com.onlyoffice.authorization.api.core.usecases.service.consent.ConsentRetrieveUsecases;
import com.onlyoffice.authorization.api.extensions.annotations.AuditAction;
import com.onlyoffice.authorization.api.extensions.annotations.DistributedRateLimiter;
import com.onlyoffice.authorization.api.web.client.APIClient;
import com.onlyoffice.authorization.api.web.client.transfer.APIClientDTOWrapper;
import com.onlyoffice.authorization.api.web.client.transfer.PersonDTO;
import com.onlyoffice.authorization.api.web.security.context.PersonContextContainer;
import com.onlyoffice.authorization.api.web.security.context.SettingsContextContainer;
import com.onlyoffice.authorization.api.web.security.context.TenantContextContainer;
import com.onlyoffice.authorization.api.web.server.transfer.request.ChangeClientActivationDTO;
import com.onlyoffice.authorization.api.web.server.transfer.request.CreateClientDTO;
import com.onlyoffice.authorization.api.web.server.transfer.request.UpdateClientDTO;
import com.onlyoffice.authorization.api.web.server.transfer.response.*;
import com.onlyoffice.authorization.api.web.server.utilities.HttpUtils;
import com.onlyoffice.authorization.api.web.server.utilities.mappers.ClientMapper;
import io.github.resilience4j.ratelimiter.annotation.RateLimiter;
import io.github.resilience4j.retry.annotation.Retry;
import jakarta.annotation.PostConstruct;
import jakarta.persistence.EntityNotFoundException;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import jakarta.validation.Valid;
import jakarta.validation.constraints.Max;
import jakarta.validation.constraints.Min;
import jakarta.validation.constraints.NotEmpty;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.data.util.Pair;
import org.springframework.http.HttpMethod;
import org.springframework.http.HttpStatus;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

import java.net.URI;
import java.time.ZoneId;
import java.util.ArrayList;
import java.util.HashSet;
import java.util.List;
import java.util.Set;
import java.util.concurrent.CompletableFuture;
import java.util.concurrent.ExecutionException;
import java.util.stream.Collectors;

import static org.springframework.hateoas.server.mvc.WebMvcLinkBuilder.linkTo;
import static org.springframework.hateoas.server.mvc.WebMvcLinkBuilder.methodOn;

/**
 *
 */
@Slf4j
@RestController
@RequestMapping(value = "/api/2.0/clients")
@RequiredArgsConstructor
public class ClientController {
    private final String AUTH_COOKIE_NAME = "asc_auth_key";

    private final ApplicationConfiguration applicationConfiguration;
    private final APIClient apiClient;

    private final ClientRetrieveUsecases retrieveUsecases;
    private final ClientCreationUsecases creationUsecases;
    private final ClientMutationUsecases mutationUsecases;
    private final ClientCleanupUsecases cleanupUsecases;
    private final ConsentRetrieveUsecases consentRetrieveUsecases;
    private final ConsentCleanupUsecases consentCleanupUsecases;
    private final AuthorizationCleanupUsecases authorizationCleanupUsecases;

    private List<String> allowedScopes = new ArrayList<>();

    /**
     *
     */
    @PostConstruct
    public void init() {
        allowedScopes = applicationConfiguration.getScopes().stream()
                .map(s -> s.getName())
                .collect(Collectors.toList());
    }

    /**
     *
     */
    private void setLoggerContextAttributes() {
        var tenant = TenantContextContainer.context.get().getResponse();
        var person = PersonContextContainer.context.get().getResponse();

        MDC.put("tenantId", String.valueOf(tenant.getTenantId()));
        MDC.put("tenantAlias", tenant.getTenantAlias());
        MDC.put("personId", person.getId());
        MDC.put("personName", person.getUserName());
    }

    /**
     *
     * @param request
     * @param response
     * @param ascAuth
     * @param page
     * @param limit
     * @return
     */
    @GetMapping
    @Retry(name = "getClientRetryRateLimiter")
    @RateLimiter(name = "getClientRateLimiter")
    @DistributedRateLimiter(name = "identityFetchClient")
    public ResponseEntity<PaginationDTO<ClientDTO>> getClients(
            HttpServletRequest request,
            HttpServletResponse response,
            @CookieValue(name = AUTH_COOKIE_NAME) String ascAuth,
            @RequestParam(value = "page") @Min(value = 0) int page,
            @RequestParam(value = "limit") @Min(value = 1) @Max(value = 100) int limit
    ) {
        var address = URI.create(HttpUtils.getRequestHostAddress(request).get());
        var tenant = TenantContextContainer.context.get().getResponse();
        var zone = ZoneId.of(SettingsContextContainer.context.get()
                .getResponse().getTimezone());
        var cookie = String.format("%s=%s", AUTH_COOKIE_NAME, ascAuth);

        try {
            MDC.put("tenantId", String.valueOf(tenant.getTenantId()));
            MDC.put("tenantAlias", tenant.getTenantAlias());
            MDC.put("page", String.valueOf(page));
            MDC.put("limit", String.valueOf(limit));
            log.info("Received a new get clients request for tenant with page and limit");
            MDC.clear();

            PaginationDTO<ClientDTO> pagination = retrieveUsecases.getTenantClients(tenant,
                    page, limit);

            var tasks = new HashSet<Pair<ClientDTO, CompletableFuture<APIClientDTOWrapper<PersonDTO>>>>();
            pagination.getData().forEach(c -> {
                MDC.put("profile", c.getModifiedBy());
                log.debug("Creating a task to get user's profile");

                tasks.add(Pair.of(c, CompletableFuture.supplyAsync(() -> apiClient
                        .getProfile(address, cookie, c.getModifiedBy()))));

                MDC.clear();
            });

            CompletableFuture.allOf(tasks.stream().map(Pair::getSecond).collect(Collectors.toSet())
                    .toArray(new CompletableFuture[tasks.size()]));

            tasks.forEach(task -> {
                var client = task.getFirst();

                MDC.put("client", client.getClientId());
                MDC.put("zone", zone.toString());
                log.debug("Setting date according to the tenant timezone");
                MDC.clear();

                client.setCreatedOn(client.getCreatedOn().toInstant().atZone(zone));
                client.setModifiedOn(client.getModifiedOn().toInstant().atZone(zone));
                try {
                    MDC.put("clientId", client.getClientId());
                    log.debug("Updating creator avatar");
                    var result = task.getSecond().get();
                    if (result != null && result.getResponse() != null) {
                        var profile = result.getResponse();
                        client.setCreatorAvatar(profile.getAvatarSmall());
                        client.setCreatorDisplayName(String
                                .format("%s %s", profile.getFirstName(), profile.getLastName()).trim());
                    }
                    MDC.clear();
                } catch (ExecutionException | InterruptedException e) {
                    MDC.put("profile", client.getModifiedBy());
                    log.error("Could not get user's profile");
                    MDC.clear();
                }
            });

            for (final var client : pagination.getData()) {
                client.add(linkTo(methodOn(ClientController.class)
                        .getClient(response, client.getClientId()))
                        .withRel(HttpMethod.GET.name())
                        .withMedia(MediaType.APPLICATION_JSON_VALUE)
                        .withTitle("get_client"));
                client.add(linkTo(methodOn(ClientController.class)
                        .updateClient(client.getClientId(), null))
                        .withRel(HttpMethod.PUT.name())
                        .withMedia(MediaType.APPLICATION_JSON_VALUE)
                        .withTitle("update_client"));
                client.add(linkTo(methodOn(ClientController.class)
                        .deleteClient(client.getClientId()))
                        .withRel(HttpMethod.DELETE.name())
                        .withTitle("delete_client"));
                client.add(linkTo(methodOn(ClientController.class)
                        .regenerateSecret(request, response, client.getClientId()))
                        .withRel(HttpMethod.PATCH.name())
                        .withTitle("regenerate_secret"));
                client.add(linkTo(methodOn(ClientController.class)
                        .activateClient(client.getClientId(), null))
                        .withRel(HttpMethod.PATCH.name())
                        .withMedia(MediaType.APPLICATION_JSON_VALUE)
                        .withTitle("activate_client"));
            }

            pagination.add(linkTo(methodOn(ClientController.class)
                    .postClient(request, response, null))
                    .withRel(HttpMethod.POST.name())
                    .withTitle("create_client"));

            return ResponseEntity.ok(pagination);
        } catch (RuntimeException e) {
            throw new EntityNotFoundException(String
                    .format("Could not find clients for tenant %s", tenant.getTenantId()), e);
        }
    }

    /**
     *
     * @param clientId
     * @return
     */
    @GetMapping("/{clientId}/info")
    @Retry(name = "getClientRetryRateLimiter")
    @RateLimiter(name = "getClientRateLimiter")
    @DistributedRateLimiter(name = "identityFetchClient")
    public ResponseEntity<ClientInfoDTO> getClientInfo(@PathVariable @NotEmpty String clientId) {
        try {
            MDC.put("clientId", clientId);
            log.info("Received a new get client info request");
            return ResponseEntity.ok(ClientMapper.INSTANCE
                .fromClientToInfoDTO(retrieveUsecases.getClient(clientId)));
        } catch (RuntimeException e) {
            throw new EntityNotFoundException(String
                    .format("Could not find client with clientId %s", clientId), e);
        } finally {
            MDC.clear();
        }
    }

    /**
     *
     * @param clientId
     * @return
     */
    @DeleteMapping("/{clientId}/revoke")
    @Retry(name = "batchClientRetryRateLimiter")
    @RateLimiter(name = "batchClientRateLimiter")
    @AuditAction(action = Action.REVOKE_USER_CLIENT)
    @DistributedRateLimiter(name = "identityMutateClient")
    public ResponseEntity revokeUserClient(@PathVariable @NotEmpty String clientId) {
        setLoggerContextAttributes();
        MDC.put("clientId", clientId);
        log.info("Received a new user revocation request");
        MDC.clear();

        consentCleanupUsecases.revokeConsentAsync(clientId, PersonContextContainer.context
                .get().getResponse().getEmail());

        return ResponseEntity.status(HttpStatus.OK).build();
    }

    /**
     *
     * @return
     */
    @GetMapping("/consents")
    @Retry(name = "getClientRetryRateLimiter")
    @RateLimiter(name = "getClientRateLimiter")
    @DistributedRateLimiter(name = "identityFetchClient")
    public ResponseEntity<Set<ConsentDTO>> getClientsInfo() {
        try {
            var zone = ZoneId.of(SettingsContextContainer.context.get()
                    .getResponse().getTimezone());

            setLoggerContextAttributes();
            MDC.put("zone", zone.toString());
            log.info("Received a new get clients info");

            var result = consentRetrieveUsecases.getAllByPrincipalName(TenantContextContainer
                    .context.get().getResponse(), PersonContextContainer.context.get()
                    .getResponse().getEmail());

            result.forEach(r -> r.setModifiedAt(r.getModifiedAt().toInstant().atZone(zone)));

            MDC.put("clients", String.valueOf(result.size()));
            log.debug("Successfully retrieved client consents");

            return ResponseEntity.ok(result);
        } catch (RuntimeException e) {
            throw new EntityNotFoundException(String
                    .format("Could not fetch consents for %s", PersonContextContainer
                            .context.get().getResponse().getEmail()), e);
        } finally {
            MDC.clear();
        }
    }

    /**
     *
     * @param response
     * @param clientId
     * @return
     */
    @GetMapping("/{clientId}")
    @Retry(name = "getClientRetryRateLimiter")
    @RateLimiter(name = "getClientRateLimiter")
    @DistributedRateLimiter(name = "identityFetchClient")
    public ResponseEntity<ClientDTO> getClient(
            HttpServletResponse response,
            @PathVariable @NotEmpty String clientId
    ) {
        try {
            var zone = ZoneId.of(SettingsContextContainer.context.get()
                    .getResponse().getTimezone());

            setLoggerContextAttributes();
            MDC.put("clientId", clientId);
            MDC.put("zone", zone.toString());
            log.info("Received a new get client request for tenant");

            var client = retrieveUsecases.getTenantClient(TenantContextContainer.context
                    .get().getResponse(), clientId);
            client.setCreatedOn(client.getCreatedOn().toInstant().atZone(zone));
            client.setModifiedOn(client.getModifiedOn().toInstant().atZone(zone));
            client.add(linkTo(methodOn(ClientController.class)
                    .updateClient(clientId, null))
                    .withRel(HttpMethod.PUT.name())
                    .withMedia(MediaType.APPLICATION_JSON_VALUE)
                    .withTitle("update_client"));
            client.add(linkTo(methodOn(ClientController.class)
                    .deleteClient(clientId))
                    .withRel(HttpMethod.DELETE.name())
                    .withTitle("delete_client"));
            client.add(linkTo(methodOn(ClientController.class)
                    .regenerateSecret(null, response, client.getClientId()))
                    .withRel(HttpMethod.PATCH.name())
                    .withTitle("regenerate_secret"));
            client.add(linkTo(methodOn(ClientController.class)
                    .postClient(null, response, null))
                    .withRel(HttpMethod.POST.name())
                    .withTitle("create_client"));
            client.add(linkTo(methodOn(ClientController.class)
                    .activateClient(clientId, null))
                    .withRel(HttpMethod.PATCH.name())
                    .withMedia(MediaType.APPLICATION_JSON_VALUE)
                    .withTitle("activate_client"));

            log.debug("Successfully found a client", client);

            return ResponseEntity.ok(client);
        } catch (RuntimeException e) {
            throw new EntityNotFoundException(String
                    .format("Could not find client with clientId %s", clientId), e);
        } finally {
            MDC.clear();
        }
    }

    /**
     *
     * @param request
     * @param response
     * @param body
     * @return
     */
    @PostMapping
    @AuditAction(action = Action.CREATE_CLIENT)
    @Retry(name = "batchClientRetryRateLimiter")
    @RateLimiter(name = "batchClientRateLimiter")
    @DistributedRateLimiter(name = "identityMutateClient")
    public ResponseEntity<ClientDTO> postClient(
            HttpServletRequest request,
            HttpServletResponse response,
            @RequestBody @Valid CreateClientDTO body
    ) {
        setLoggerContextAttributes();
        log.info("Received a new create client request", body);

        if (!body.getScopes().stream()
                .allMatch(s -> allowedScopes.contains(s))) {
            log.error("Could not create a new client with unsupported scopes");
            MDC.clear();
            return ResponseEntity.status(HttpStatus.BAD_REQUEST).build();
        }

        log.debug("Submitting create new client message");

        var client = creationUsecases.createClientAsync(TenantContextContainer
                .context.get().getResponse(), body, PersonContextContainer.context.
                get().getResponse(), HttpUtils.getRequestHostAddress(request).get());
        client.add(linkTo(methodOn(ClientController.class)
                .getClient(response, client.getClientId()))
                .withRel(HttpMethod.GET.name())
                .withMedia(MediaType.APPLICATION_JSON_VALUE)
                .withTitle("get_client"));
        client.add(linkTo(methodOn(ClientController.class)
                .updateClient(client.getClientId(),null))
                .withRel(HttpMethod.PUT.name())
                .withMedia(MediaType.APPLICATION_JSON_VALUE)
                .withTitle("update_client"));
        client.add(linkTo(methodOn(ClientController.class)
                .deleteClient(client.getClientId()))
                .withRel(HttpMethod.DELETE.name())
                .withTitle("delete_client"));
        client.add(linkTo(methodOn(ClientController.class)
                .regenerateSecret(request, response, client.getClientId()))
                .withRel(HttpMethod.PATCH.name())
                .withTitle("regenerate_secret"));
        client.add(linkTo(methodOn(ClientController.class)
                .activateClient(client.getClientId(), null))
                .withRel(HttpMethod.PATCH.name())
                .withMedia(MediaType.APPLICATION_JSON_VALUE)
                .withTitle("activate_client"));

        log.debug("Successfully submitted create new client message and generated a new client", client);
        MDC.clear();

        return ResponseEntity.status(HttpStatus.CREATED).body(client);
    }

    /**
     *
     * @param clientId
     * @param body
     * @return
     */
    @PutMapping("/{clientId}")
    @AuditAction(action = Action.UPDATE_CLIENT)
    @Retry(name = "updateClientRetryRateLimiter")
    @RateLimiter(name = "updateClientRateLimiter")
    @DistributedRateLimiter(name = "identityMutateClient")
    public ResponseEntity updateClient(
            @PathVariable @NotEmpty String clientId,
            @RequestBody @Valid UpdateClientDTO body
    ) {
        setLoggerContextAttributes();
        MDC.put("clientId", clientId);
        log.info("Received a new update client request");
        log.debug("Submitting update client message", body);

        mutationUsecases.updateClientAsync(TenantContextContainer.context
                .get().getResponse(), body, clientId);

        log.debug("Successfully submitted update client message");
        MDC.clear();

        return ResponseEntity.ok().build();
    }

    /**
     *
     * @param request
     * @param response
     * @param clientId
     * @return
     */
    @PatchMapping("/{clientId}/regenerate")
    @AuditAction(action = Action.REGENERATE_SECRET)
    @Retry(name = "regenerateClientSecretRetryRateLimiter")
    @RateLimiter(name = "regenerateClientSecretRateLimiter")
    @DistributedRateLimiter(name = "identityMutateClient")
    public ResponseEntity<SecretDTO> regenerateSecret(
            HttpServletRequest request,
            HttpServletResponse response,
            @PathVariable @NotEmpty String clientId
    ) {
        var tenant = TenantContextContainer.context.get().getResponse();

        setLoggerContextAttributes();
        MDC.put("clientId", clientId);
        log.info("Received a new regenerate client's secret request");

        try {
            var regenerate = CompletableFuture.supplyAsync(() -> authorizationCleanupUsecases
                            .deleteTenantAuthorizationsByClientId(tenant, clientId))
                    .thenApplyAsync((r) -> mutationUsecases.regenerateSecret(tenant, clientId))
                    .get();
            regenerate.add(linkTo(methodOn(ClientController.class)
                    .getClient(response, clientId))
                    .withRel(HttpMethod.GET.name())
                    .withMedia(MediaType.APPLICATION_JSON_VALUE)
                    .withTitle("get_client"));
            regenerate.add(linkTo(methodOn(ClientController.class)
                    .updateClient(clientId, null))
                    .withRel(HttpMethod.PUT.name())
                    .withMedia(MediaType.APPLICATION_JSON_VALUE)
                    .withTitle("update_client"));
            regenerate.add(linkTo(methodOn(ClientController.class)
                    .deleteClient(clientId))
                    .withRel(HttpMethod.DELETE.name())
                    .withTitle("delete_client"));
            regenerate.add(linkTo(methodOn(ClientController.class)
                    .postClient(request, response, null))
                    .withRel(HttpMethod.POST.name())
                    .withTitle("create_client"));
            regenerate.add(linkTo(methodOn(ClientController.class)
                    .activateClient(clientId, null))
                    .withRel(HttpMethod.PATCH.name())
                    .withMedia(MediaType.APPLICATION_JSON_VALUE)
                    .withTitle("activate_client"));

            log.debug("Secret regeneration result", regenerate);

            return ResponseEntity.ok(regenerate);
        } catch (InterruptedException | ExecutionException e) {
            throw new EntityCreationException("Could not regenerate client's secret", e);
        } finally {
            MDC.clear();
        }
    }

    /**
     *
     * @param clientId
     * @return
     */
    @DeleteMapping("/{clientId}")
    @AuditAction(action = Action.DELETE_CLIENT)
    @Retry(name = "batchClientRetryRateLimiter")
    @RateLimiter(name = "batchClientRateLimiter")
    @DistributedRateLimiter(name = "identityMutateClient")
    public ResponseEntity deleteClient(@PathVariable @NotEmpty String clientId) {
        setLoggerContextAttributes();
        MDC.put("clientId", clientId);
        log.info("Received a new delete client request for tenant");
        MDC.clear();

        cleanupUsecases.deleteClientAsync(TenantContextContainer.context
                .get().getResponse(), clientId);

        return ResponseEntity.status(HttpStatus.OK).build();
    }

    /**
     *
     * @param clientId
     * @param body
     * @return
     */
    @PatchMapping("/{clientId}/activation")
    @AuditAction(action = Action.CHANGE_CLIENT_ACTIVATION)
    @Retry(name = "regenerateClientSecretRetryRateLimiter")
    @RateLimiter(name = "regenerateClientSecretRateLimiter")
    @DistributedRateLimiter(name = "identityMutateClient")
    public ResponseEntity activateClient(
            @PathVariable @NotEmpty String clientId,
            @RequestBody @Valid ChangeClientActivationDTO body
    ) {
        try {
            setLoggerContextAttributes();
            MDC.put("clientId", clientId);
            log.info("Received a new change client activation request for tenant");

            if (mutationUsecases.changeActivation(TenantContextContainer.context
                    .get().getResponse(), body, clientId))
                return ResponseEntity.status(HttpStatus.OK).build();

            return ResponseEntity.badRequest().build();
        } catch (RuntimeException e) {
            throw new UnsupportedOperationException(String
                    .format("Could not change client's activation with clientId %s", clientId), e);
        } finally {
            MDC.clear();
        }
    }
}

