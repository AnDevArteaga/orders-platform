namespace Orders.Domain.Exceptions;

public class NotFoundException(string message) : Exception(message);

public class ConflictException(string message) : Exception(message);

public class ForbiddenException(string message) : Exception(message);
