---
description: "This file provides guidelines for writing clean, maintainable, and idiomatic C# code with a focus on functional patterns and proper abstraction."
---
# Role Definition:

- C# Language Expert
- Software Architect
- Code Quality Specialist

## General:

**Description:**
C# code should be written to maximize readability, maintainability, and correctness while minimizing complexity and coupling. Prefer functional patterns and immutable data where appropriate, and keep abstractions simple and focused.

**Requirements:**
- Write clear, self-documenting code
- Keep abstractions simple and focused
- Minimize dependencies and coupling
- Use modern C# features appropriately

## Code Organization:

- Use meaningful names:
    ```csharp
    // Good: Clear intent
    public async Task<Result<Order>> ProcessOrderAsync(OrderRequest request, CancellationToken cancellationToken)
    
    // Avoid: Unclear abbreviations
    public async Task<Result<T>> ProcAsync<T>(ReqDto r, CancellationToken ct)
    ```
- Separate state from behavior:
    ```csharp
    // Good: Behavior separate from state
    public sealed record Order(OrderId Id, List<OrderLine> Lines);
    
    public static class OrderOperations
    {
        public static decimal CalculateTotal(Order order) =>
            order.Lines.Sum(line => line.Price * line.Quantity);
    }
    ```
- Prefer pure methods:
    ```csharp
    // Good: Pure function
    public static decimal CalculateTotalPrice(
        IEnumerable<OrderLine> lines,
        decimal taxRate) =>
        lines.Sum(line => line.Price * line.Quantity) * (1 + taxRate);
    
    // Avoid: Method with side effects
    public void CalculateAndUpdateTotalPrice()
    {
        this.Total = this.Lines.Sum(l => l.Price * l.Quantity);
        this.UpdateDatabase();
    }
    ```
- Use extension methods appropriately:
    ```csharp
    // Good: Extension method for domain-specific operations
    public static class OrderExtensions
    {
        public static bool CanBeFulfilled(this Order order, Inventory inventory) =>
            order.Lines.All(line => inventory.HasStock(line.ProductId, line.Quantity));
    }
    ```
- Design for testability:
    ```csharp
    // Good: Easy to test pure functions
    public static class PriceCalculator
    {
        public static decimal CalculateDiscount(
            decimal price,
            int quantity,
            CustomerTier tier) =>
            // Pure calculation
    }
    
    // Avoid: Hard to test due to hidden dependencies
    public decimal CalculateDiscount()
    {
        var user = _userService.GetCurrentUser();  // Hidden dependency
        var settings = _configService.GetSettings(); // Hidden dependency
        // Calculation
    }
    ```

## Dependency Management:

- Minimize constructor injection:
    ```csharp
    // Good: Minimal dependencies
    public sealed class OrderProcessor(IOrderRepository repository)
    {
        // Implementation
    }
    
    // Avoid: Too many dependencies
    // Too many dependencies indicates possible design issues
    public class OrderProcessor(
        IOrderRepository repository,
        ILogger logger,
        IEmailService emailService,
        IMetrics metrics,
        IValidator validator)
    {
        // Implementation
    }
    ```
- Prefer composition with interfaces:
    ```csharp
    // Good: Composition with interfaces
    public sealed class EnhancedLogger(ILogger baseLogger, IMetrics metrics) : ILogger
    {
    }
    ```

---
description: "This file provides guidelines for writing clean, maintainable, and idiomatic C# code with a focus on functional patterns and proper abstraction."
---

## Type Definitions:

- Prefer records for data types:
    ```csharp
    // Good: Immutable data type with value semantics
    public sealed record CustomerDto(string Name, Email Email);
    
    // Avoid: Class with mutable properties
    public class Customer
    {
        public string Name { get; set; }
        public string Email { get; set; }
    }
    ```
- Make classes sealed by default:
    ```csharp
    // Good: Make classes sealed by default
    public sealed class OrderProcessor
    {
        // Implementation
    }
    
    // Only unsealed when inheritance is specifically designed for
    public abstract class Repository<T>
    {
        // Base implementation
    }
    ```

## Variable Declarations:

- Use var where possible:
    ```csharp
    // Good: Using var for type inference
    var fruit = "Apple";
    var number = 42;
    var order = new Order(fruit, number);
    ```

## Control Flow:

- Prefer range indexers over LINQ:
    ```csharp
    // Good: Using range indexers with clear comments
    var lastItem = items[^1];  // ^1 means "1 from the end"
    var firstThree = items[..3];  // ..3 means "take first 3 items"
    var slice = items[2..5];  // take items from index 2 to 4 (5 exclusive)
    
    // Avoid: Using LINQ when range indexers are clearer
    var lastItem = items.LastOrDefault();
    var firstThree = items.Take(3).ToList();
    var slice = items.Skip(2).Take(3).ToList();
    ```
- Prefer collection initializers:
    ```csharp
    // Good: Using collection initializers
    string[] fruits = ["Apple", "Banana", "Cherry"];
    
    // Avoid: Using explicit initialization when type is clear
    var fruits = new List<int>() {
        "Apple",
        "Banana",
        "Cherry"
    };
    ```
- Use pattern matching effectively:
    ```csharp
    // Good: Clear pattern matching
    public decimal CalculateDiscount(Customer customer) =>
        customer switch
        {
            { Tier: CustomerTier.Premium } => 0.2m,
            { OrderCount: > 10 } => 0.1m,
            _ => 0m
        };
    
    // Avoid: Nested if statements
    public decimal CalculateDiscount(Customer customer)
    {
        if (customer.Tier == CustomerTier.Premium)
            return 0.2m;
        if (customer.OrderCount > 10)
            return 0.1m;
        return 0m;
    }
    ```

## Nullability:

- Mark nullable fields explicitly:
    ```csharp
    // Good: Explicit nullability
    public class OrderProcessor
    {
        private readonly ILogger<OrderProcessor>? _logger;
        private string? _lastError;
        
        public OrderProcessor(ILogger<OrderProcessor>? logger = null)
        {
            _logger = logger;
        }
    }
    
    // Avoid: Implicit nullability
    public class OrderProcessor
    {
        private readonly ILogger<OrderProcessor> _logger; // Warning: Could be null
        private string _lastError; // Warning: Could be null
    }
    ```
- Use null checks only when necessary for reference types and public methods:
    ```csharp
    // Good: Proper null checking
    public void ProcessOrder(Order order)
    {
        ArgumentNullException.ThrowIfNull(order); // Appropriate for reference types

        _logger?.LogInformation("Processing order {Id}", order.Id);
    }
    
    // Good: Using pattern matching for null checks
    public decimal CalculateTotal(Order? order) =>
        order switch
        {
            null => throw new ArgumentNullException(nameof(order)),
            { Lines: null } => throw new ArgumentException("Order lines cannot be null", nameof(order)),
            _ => order.Lines.Sum(l => l.Total)
        };
    // BAD: Avoid null checks for value types
    public void ProcessOrder(int orderId)
    {
        ArgumentNullException.ThrowIfNull(order); // DON'T USE Null checks are unnecessary for value types
    }

    // Avoid: null checks for non-public methods
    private void ProcessOrder(Order order)
    {
        ArgumentNullException.ThrowIfNull(order); // DON'T USE, ProcessOrder is private
    }
    ```
- Use null-forgiving operator when appropriate:
    ```csharp
    public class OrderValidator
    {
        private readonly IValidator<Order> _validator;
        
        public OrderValidator(IValidator<Order> validator)
        {
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }
        
        public ValidationResult Validate(Order order)
        {
            // We know _validator can't be null due to constructor check
            return _validator!.Validate(order);
        }
    }
    ```
- Use nullability attributes:
    ```csharp
    public class StringUtilities
    {
        // Output is non-null if input is non-null
        [return: NotNullIfNotNull(nameof(input))]
        public static string? ToUpperCase(string? input) =>
            input?.ToUpperInvariant();
        
        // Method never returns null
        [return: NotNull]
        public static string EnsureNotNull(string? input) =>
            input ?? string.Empty;
        
        // Parameter must not be null when method returns true
        public static bool TryParse(string? input, [NotNullWhen(true)] out string? result)
        {
            result = null;
            if (string.IsNullOrEmpty(input))
                return false;
                
            result = input;
            return true;
        }
    }
    ```
- Use init-only properties with non-null validation:
    ```csharp
    // Good: Non-null validation in constructor
    public sealed record Order
    {
        public required OrderId Id { get; init; }
        public required ImmutableList<OrderLine> Lines { get; init; }
        
        public Order()
        {
            Id = null!; // Will be set by required property
            Lines = null!; // Will be set by required property
        }
        
        private Order(OrderId id, ImmutableList<OrderLine> lines)
        {
            Id = id;
            Lines = lines;
        }
        
        public static Order Create(OrderId id, IEnumerable<OrderLine> lines) =>
            new(id, lines.ToImmutableList());
    }
    ```
- Document nullability in interfaces:
    ```csharp
    public interface IOrderRepository
    {
        // Explicitly shows that null is a valid return value
        Task<Order?> FindByIdAsync(OrderId id, CancellationToken ct = default);
        
        // Method will never return null
        [return: NotNull]
        Task<IReadOnlyList<Order>> GetAllAsync(CancellationToken ct = default);
        
        // Parameter cannot be null
        Task SaveAsync([NotNull] Order order, CancellationToken ct = default);
    }
    ```

## Safe Operations:

- Use Try methods for safer operations:
    ```csharp
    // Good: Using TryGetValue for dictionary access
    if (dictionary.TryGetValue(key, out var value))
    {
        // Use value safely here
    }
    else
    {
        // Handle missing key case
    }
    ```
    ```csharp
    // Avoid: Direct indexing which can throw
    var value = dictionary[key];  // Throws if key doesn't exist

    // Good: Using Uri.TryCreate for URL parsing
    if (Uri.TryCreate(urlString, UriKind.Absolute, out var uri))
    {
        // Use uri safely here
    }
    else
    {
        // Handle invalid URL case
    }
    ```
    ```csharp
    // Avoid: Direct Uri creation which can throw
    var uri = new Uri(urlString);  // Throws on invalid URL

    // Good: Using int.TryParse for number parsing
    if (int.TryParse(input, out var number))
    {
        // Use number safely here
    }
    else
    {
        // Handle invalid number case
    }
    ```
    ```csharp
    // Good: Combining Try methods with null coalescing
    var value = dictionary.TryGetValue(key, out var result)
        ? result
        : defaultValue;

    // Good: Using Try methods in LINQ with pattern matching
    var validNumbers = strings
        .Select(s => (Success: int.TryParse(s, out var num), Value: num))
        .Where(x => x.Success)
        .Select(x => x.Value);
    ```

- Prefer Try methods over exception handling:
    ```csharp
    // Good: Using Try method
    if (decimal.TryParse(priceString, out var price))
    {
        // Process price
    }

    // Avoid: Exception handling for expected cases
    try
    {
        var price = decimal.Parse(priceString);
        // Process price
    }
    catch (FormatException)
    {
        // Handle invalid format
    }
    ```

## Asynchronous Programming:

- Use Task.FromResult for pre-computed values:
    ```csharp
    // Good: Return pre-computed value
    public Task<int> GetDefaultQuantityAsync() =>
        Task.FromResult(1);
    
    // Better: Use ValueTask for zero allocations
    public ValueTask<int> GetDefaultQuantityAsync() =>
        new ValueTask<int>(1);
    
    // Avoid: Unnecessary thread pool usage
    public Task<int> GetDefaultQuantityAsync() =>
        Task.Run(() => 1);
    ```
- Always flow CancellationToken:
    ```csharp
    // Good: Propagate cancellation
    public async Task<Order> ProcessOrderAsync(
        OrderRequest request,
        CancellationToken cancellationToken)
    {
        var order = await _repository.GetAsync(
            request.OrderId, 
            cancellationToken);
            
        await _processor.ProcessAsync(
            order, 
            cancellationToken);
            
        return order;
    }
    ```
- Prefer await:
    ```csharp
    // Good: Using await
    public async Task<Order> ProcessOrderAsync(OrderId id)
    {
        var order = await _repository.GetAsync(id);
        await _validator.ValidateAsync(order);
        return order;
    }
    ```
- Never use Task.Result or Task.Wait:
    ```csharp
    // Good: Async all the way
    public async Task<Order> GetOrderAsync(OrderId id)
    {
        return await _repository.GetAsync(id);
    }
    
    // Avoid: Blocking on async code
    public Order GetOrder(OrderId id)
    {
        return _repository.GetAsync(id).Result; // Can deadlock
    }
    ```
- Use TaskCompletionSource correctly:
    ```csharp
    // Good: Using RunContinuationsAsynchronously
    private readonly TaskCompletionSource<Order> _tcs = 
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    
    // Avoid: Default TaskCompletionSource can cause deadlocks
    private readonly TaskCompletionSource<Order> _tcs = new();
    ```
- Always dispose CancellationTokenSources:
    ```csharp
    // Good: Proper disposal of CancellationTokenSource
    public async Task<Order> GetOrderWithTimeout(OrderId id)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        return await _repository.GetAsync(id, cts.Token);
    }
    ```
- Prefer async/await over direct Task return:
    ```csharp
    // Good: Using async/await
    public async Task<Order> ProcessOrderAsync(OrderRequest request)
    {
        await _validator.ValidateAsync(request);
        var order = await _factory.CreateAsync(request);
        return order;
    }
    
    // Avoid: Manual task composition
    public Task<Order> ProcessOrderAsync(OrderRequest request)
    {
        return _validator.ValidateAsync(request)
            .ContinueWith(t => _factory.CreateAsync(request))
            .Unwrap();
    }
    ```

## Symbol References:

- Always use nameof operator:
    ```csharp
    // Good: Using nameof in attributes
    public class OrderProcessor
    {
        [Required(ErrorMessage = "The {0} field is required")]
        [Display(Name = nameof(OrderId))]
        public string OrderId { get; init; }
        
        [MemberNotNull(nameof(_repository))]
        private void InitializeRepository()
        {
            _repository = new OrderRepository();
        }
        
        [NotifyPropertyChangedFor(nameof(FullName))]
        public string FirstName
        {
            get => _firstName;
            set => SetProperty(ref _firstName, value);
        }
    }
    ```
- Use nameof with exceptions:
    ```csharp
    public class OrderService
    {
        public async Task<Order> GetOrderAsync(OrderId id, CancellationToken ct)
        {
            var order = await _repository.FindAsync(id, ct);
            
            if (order is null)
                throw new OrderNotFoundException(
                    $"Order with {nameof(id)} '{id}' not found");
                    
            if (!order.Lines.Any())
                throw new InvalidOperationException(
                    $"{nameof(order.Lines)} cannot be empty");
                    
            return order;
        }
        
        public void ValidateOrder(Order order)
        {
            if (order.Lines.Count == 0)
                throw new ArgumentException(
                    "Order must have at least one line",
                    nameof(order));
        }
    }
    ```
- Use nameof in logging:
    ```csharp
    public class OrderProcessor
    {
        private readonly ILogger<OrderProcessor> _logger;
        
        public async Task ProcessAsync(Order order)
        {
            _logger.LogInformation(
                "Starting {Method} for order {OrderId}",
                nameof(ProcessAsync),
                order.Id);
                
            try
            {
                await ProcessInternalAsync(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error in {Method} for {Property} {Value}",
                    nameof(ProcessAsync),
                    nameof(order.Id),
                    order.Id);
                throw;
            }
        }
    }
    ```

### Usings and Namespaces:

- Use implicit usings:
    ```csharp
    // Good: Implicit
    namespace MyNamespace
    {
        public class MyClass
        {
            // Implementation
        }
    }
    // Avoid:
    using System; // DON'T USE
    using System.Collections.Generic; // DON'T USE
    using System.IO; // DON'T USE
    using System.Linq; // DON'T USE
    using System.Net.Http; // DON'T USE
    using System.Threading; // DON'T USE
    using System.Threading.Tasks;// DON'T USE
    using System.Net.Http.Json; // DON'T USE
    using Microsoft.AspNetCore.Builder; // DON'T USE
    using Microsoft.AspNetCore.Hosting; // DON'T USE
    using Microsoft.AspNetCore.Http; // DON'T USE
    using Microsoft.AspNetCore.Routing; // DON'T USE
    using Microsoft.Extensions.Configuration; // DON'T USE
    using Microsoft.Extensions.DependencyInjection; // DON'T USE
    using Microsoft.Extensions.Hosting; // DON'T USE
    using Microsoft.Extensions.Logging; // DON'T USE
    using Good: Explicit usings; // DON'T USE
    
    namespace MyNamespace
    {
        public class MyClass
        {
            // Implementation
        }
    }
    ```
- Use file-scoped namespaces:
    ```csharp
    // Good: File-scoped namespace
    namespace MyNamespace;
    
    public class MyClass
    {
        // Implementation
    }
    
    // Avoid: Block-scoped namespace
    namespace MyNamespace
    {
        public class MyClass
        {
            // Implementation
        }
    }
    ```


---
description: "This file provides guidelines for writing effective, maintainable tests using xUnit and related tools"
applyTo: "tests/**/*.cs"
---

## Role Definition:
- Test Engineer
- Quality Assurance Specialist

## General:

**Description:**
Tests should be reliable, maintainable, and provide meaningful coverage. Use xUnit as the primary testing framework, with proper isolation and clear patterns for test organization and execution.

**Requirements:**
- Use xUnit as the testing framework
- Ensure test isolation
- Follow consistent patterns
- Maintain high code coverage

## Test Class Structure:

- Use ITestOutputHelper for logging:
    ```csharp
    public class OrderProcessingTests(ITestOutputHelper output)
    {
        
        [Fact]
        public async Task ProcessOrder_ValidOrder_Succeeds()
        {
            output.WriteLine("Starting test with valid order");
            // Test implementation
        }
    }
    ```
- Use fixtures for shared state:
    ```csharp
    public class DatabaseFixture : IAsyncLifetime
    {
        public DbConnection Connection { get; private set; }
        
        public async Task InitializeAsync()
        {
            Connection = new SqlConnection("connection-string");
            await Connection.OpenAsync();
        }
        
        public async Task DisposeAsync()
        {
            await Connection.DisposeAsync();
        }
    }
    
    public class OrderTests : IClassFixture<DatabaseFixture>
    {
        private readonly DatabaseFixture _fixture;
        private readonly ITestOutputHelper _output;
        
        public OrderTests(DatabaseFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _output = output;
        }
    }
    ```

## Test Methods:

- Prefer Theory over multiple Facts:
    ```csharp
    public class DiscountCalculatorTests
    {
        public static TheoryData<decimal, int, decimal> DiscountTestData => 
            new()
            {
                { 100m, 1, 0m },      // No discount for single item
                { 100m, 5, 5m },      // 5% for 5 items
                { 100m, 10, 10m },    // 10% for 10 items
            };
        
        [Theory]
        [MemberData(nameof(DiscountTestData))]
        public void CalculateDiscount_ReturnsCorrectAmount(
            decimal price,
            int quantity,
            decimal expectedDiscount)
        {
            // Arrange
            var calculator = new DiscountCalculator();
            
            // Act
            var discount = calculator.Calculate(price, quantity);
            
            // Assert
            Assert.Equal(expectedDiscount, discount);
        }
    }
    ```
- Follow Arrange-Act-Assert pattern:
    ```csharp
    [Fact]
    public async Task ProcessOrder_ValidOrder_UpdatesInventory()
    {
        // Arrange
        var order = new Order(
            OrderId.New(),
            new[] { new OrderLine("SKU123", 5) });
        var processor = new OrderProcessor(_mockRepository.Object);
        
        // Act
        var result = await processor.ProcessAsync(order);
        
        // Assert
        Assert.True(result.IsSuccess);
        _mockRepository.Verify(
            r => r.UpdateInventoryAsync(
                It.IsAny<string>(),
                It.IsAny<int>()),
            Times.Once);
    }
    ```

## Test Isolation:

- Use fresh data for each test:
    ```csharp
    public class OrderTests
    {
        private static Order CreateTestOrder() =>
            new(OrderId.New(), TestData.CreateOrderLines());
            
        [Fact]
        public async Task ProcessOrder_Success()
        {
            var order = CreateTestOrder();
            // Test implementation
        }
    }
    ```
- Clean up resources:
    ```csharp
    public class IntegrationTests : IAsyncDisposable
    {
        private readonly TestServer _server;
        private readonly HttpClient _client;
        
        public IntegrationTests()
        {
            _server = new TestServer(CreateHostBuilder());
            _client = _server.CreateClient();
        }
        
        public async ValueTask DisposeAsync()
        {
            _client.Dispose();
            await _server.DisposeAsync();
        }
    }
    ```

## Best Practices:

- Name tests clearly:
    ```csharp
    // Good: Clear test names
    [Fact]
    public async Task ProcessOrder_WhenInventoryAvailable_UpdatesStockAndReturnsSuccess()
    
    // Avoid: Unclear names
    [Fact]
    public async Task TestProcessOrder()
    ```
- Use meaningful assertions:
    ```csharp
    // Good: Clear assertions
    Assert.Equal(expected, actual);
    Assert.Contains(expectedItem, collection);
    Assert.Throws<OrderException>(() => processor.Process(invalidOrder));
    
    // Avoid: Multiple assertions without context
    Assert.NotNull(result);
    Assert.True(result.Success);
    Assert.Equal(0, result.Errors.Count);
    ```
- Handle async operations properly:
    ```csharp
    // Good: Async test method
    [Fact]
    public async Task ProcessOrder_ValidOrder_Succeeds()
    {
        await using var processor = new OrderProcessor();
        var result = await processor.ProcessAsync(order);
        Assert.True(result.IsSuccess);
    }
    
    // Avoid: Sync over async
    [Fact]
    public void ProcessOrder_ValidOrder_Succeeds()
    {
        using var processor = new OrderProcessor();
        var result = processor.ProcessAsync(order).Result;  // Can deadlock
        Assert.True(result.IsSuccess);
    }
    ```
- Use `TestContext.Current.CancellationToken` for cancellation:
    ```csharp
    // Good:
    [Fact]
    public async Task ProcessOrder_CancellationRequested()
    {
        await using var processor = new OrderProcessor();
        var result = await processor.ProcessAsync(order, TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess);
    }
    // Avoid:
    [Fact]
    public async Task ProcessOrder_CancellationRequested()
    {
        await using var processor = new OrderProcessor();
        var result = await processor.ProcessAsync(order, CancellationToken.None);
        Assert.False(result.IsSuccess);
    }
    ```

## Assertions:

- Use xUnit's built-in assertions:
    ```csharp
    // Good: Using xUnit's built-in assertions
    public class OrderTests
    {
        [Fact]
        public void CalculateTotal_WithValidLines_ReturnsCorrectSum()
        {
            // Arrange
            var order = new Order(
                OrderId.New(),
                new[]
                {
                    new OrderLine("SKU1", 2, 10.0m),
                    new OrderLine("SKU2", 1, 20.0m)
                });
            
            // Act
            var total = order.CalculateTotal();
            
            // Assert
            Assert.Equal(40.0m, total);
        }
        
        [Fact]
        public void Order_WithInvalidLines_ThrowsException()
        {
            // Arrange
            var invalidLines = new OrderLine[] { };
            
            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() =>
                new Order(OrderId.New(), invalidLines));
            Assert.Equal("Order must have at least one line", ex.Message);
        }
        
        [Fact]
        public void Order_WithValidData_HasExpectedProperties()
        {
            // Arrange
            var id = OrderId.New();
            var lines = new[] { new OrderLine("SKU1", 1, 10.0m) };
            
            // Act
            var order = new Order(id, lines);
            
            // Assert
            Assert.NotNull(order);
            Assert.Equal(id, order.Id);
            Assert.Single(order.Lines);
            Assert.Collection(order.Lines,
                line =>
                {
                    Assert.Equal("SKU1", line.Sku);
                    Assert.Equal(1, line.Quantity);
                    Assert.Equal(10.0m, line.Price);
                });
        }
    }
    ```
    
- Avoid third-party assertion libraries:
    ```csharp
    // Avoid: Using FluentAssertions or similar libraries
    public class OrderTests
    {
        [Fact]
        public void CalculateTotal_WithValidLines_ReturnsCorrectSum()
        {
            var order = new Order(
                OrderId.New(),
                new[]
                {
                    new OrderLine("SKU1", 2, 10.0m),
                    new OrderLine("SKU2", 1, 20.0m)
                });
            
            // Avoid: Using FluentAssertions
            order.CalculateTotal().Should().Be(40.0m);
            order.Lines.Should().HaveCount(2);
            order.Should().NotBeNull();
        }
    }
    ```
    
- Use proper assertion types:
    ```csharp
    public class CustomerTests
    {
        [Fact]
        public void Customer_WithValidEmail_IsCreated()
        {
            // Boolean assertions
            Assert.True(customer.IsActive);
            Assert.False(customer.IsDeleted);
            
            // Equality assertions
            Assert.Equal("john@example.com", customer.Email);
            Assert.NotEqual(Guid.Empty, customer.Id);
            
            // Collection assertions
            Assert.Empty(customer.Orders);
            Assert.Contains("Admin", customer.Roles);
            Assert.DoesNotContain("Guest", customer.Roles);
            Assert.All(customer.Orders, o => Assert.NotNull(o.Id));
            
            // Type assertions
            Assert.IsType<PremiumCustomer>(customer);
            Assert.IsAssignableFrom<ICustomer>(customer);
            
            // String assertions
            Assert.StartsWith("CUST", customer.Reference);
            Assert.Contains("Premium", customer.Description);
            Assert.Matches("^CUST\\d{6}$", customer.Reference);
            
            // Range assertions
            Assert.InRange(customer.Age, 18, 100);
            
            // Reference assertions
            Assert.Same(expectedCustomer, actualCustomer);
            Assert.NotSame(differentCustomer, actualCustomer);
        }
    }
    ```
    
- Use Assert.Collection for complex collections:
    ```csharp
    [Fact]
    public void ProcessOrder_CreatesExpectedEvents()
    {
        // Arrange
        var processor = new OrderProcessor();
        var order = CreateTestOrder();
        
        // Act
        var events = processor.Process(order);
        
        // Assert
        Assert.Collection(events,
            evt =>
            {
                Assert.IsType<OrderReceivedEvent>(evt);
                var received = Assert.IsType<OrderReceivedEvent>(evt);
                Assert.Equal(order.Id, received.OrderId);
            },
            evt =>
            {
                Assert.IsType<InventoryReservedEvent>(evt);
                var reserved = Assert.IsType<InventoryReservedEvent>(evt);
                Assert.Equal(order.Id, reserved.OrderId);
                Assert.NotEmpty(reserved.ReservedItems);
            },
            evt =>
            {
                Assert.IsType<OrderConfirmedEvent>(evt);
                var confirmed = Assert.IsType<OrderConfirmedEvent>(evt);
                Assert.Equal(order.Id, confirmed.OrderId);
                Assert.True(confirmed.IsSuccess);
            });
    }
    ```