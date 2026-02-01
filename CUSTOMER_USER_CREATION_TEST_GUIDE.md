# Customer & User Creation Test Guide

## ?? What We're Testing

After fixing the Blazor buttons issue, we need to verify:
1. **Add Customer** button works
2. **Customer form** saves data correctly
3. **User Registration** works (if enabled)
4. **Form validation** is functioning

---

## ? Test 1: Add Customer Button

### Steps:
1. **Navigate to Customers**
   - URL: `http://your-server:7159/customers`
   - Or click "Customers" in the navigation menu

2. **Click "Add Customer" button**
   - Should be in the top-right corner
   - **Expected:** Page navigates to `/customers/new`
   - **If fails:** Button doesn't respond = Blazor still broken

3. **Verify Form Loads**
   - Should see "Add New Customer" heading
   - Form fields should be visible:
     - First Name
     - Last Name
     - Company Name
     - Email
     - Phone
     - Address fields

---

## ? Test 2: Create New Customer

### Test Data:
```
First Name: John
Last Name: Doe
Company: ABC Plumbing
Email: john.doe@example.com
Phone: 555-123-4567
Address: 123 Main St
City: Springfield
State: IL
Zip: 62701
Customer Type: Residential
```

### Steps:
1. **Fill in required fields** (marked with *)
   - First Name *
   - Last Name *
   - Phone *

2. **Fill optional fields**
   - Email
   - Company Name
   - Address info

3. **Select Customer Type**
   - Dropdown: Residential, Commercial, Property Manager, or Government

4. **Click "Save" button**
   - **Expected:** 
     - Form submits successfully
     - Redirects to customer detail page or list
     - Success message appears
   - **If fails:**
     - Check browser console for errors
     - Verify validation messages appear

---

## ? Test 3: Verify Customer Was Created

### Steps:
1. **Go back to customer list**
   - Click "Customers" in navigation
   - Or URL: `/customers`

2. **Search for the customer**
   - Use search box: "John Doe"
   - **Expected:** Customer appears in list

3. **Click on the customer**
   - **Expected:** Detail page shows all entered data

4. **Check database (optional)**
   ```sh
   docker exec -it tradeflow-db /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "YourPassword" -C -Q "SELECT TOP 5 * FROM TradeFlowFSM.dbo.Customers ORDER BY CreatedAt DESC"
   ```

---

## ? Test 4: Edit Customer

### Steps:
1. **From customer detail page**, click "Edit" button
   - **Expected:** Navigates to `/customers/{id}/edit`
   - Form loads with existing data

2. **Modify a field**
   - Change phone to: `555-987-6543`

3. **Click "Save"**
   - **Expected:**
     - Saves successfully
     - Shows updated data

---

## ? Test 5: Form Validation

### Steps:
1. **Go to Add Customer** page
2. **Leave required fields empty**
3. **Click "Save"**
   - **Expected:** Validation messages appear
     - "First Name is required"
     - "Last Name is required"
     - "Phone is required"

4. **Enter invalid email**
   - Email: `not-an-email`
   - **Expected:** "Invalid email format" message

5. **Enter invalid phone**
   - Phone: `123` (too short)
   - **Expected:** Validation message or auto-format

---

## ? Test 6: User Registration (Admin Accounts)

### ?? Note: Registration might be disabled in production

### Steps:
1. **Navigate to** `/Account/Register`
   - Or log out and click "Register"

2. **Fill registration form:**
   ```
   Email: testuser@example.com
   Password: TestPassword123!
   Confirm Password: TestPassword123!
   ```

3. **Click "Register" button**
   - **Expected:**
     - Account created successfully
     - Auto-login or redirect to login
   - **If disabled:**
     - Shows message "Registration is disabled"
     - This is normal for production

---

## ? Test 7: Dark Mode Toggle (Bonus)

Since we fixed the buttons, let's verify dark mode works:

### Steps:
1. **Click moon/sun icon** in top-right
   - **Expected:** Theme switches between light and dark
   - Page colors change instantly

2. **Reload page**
   - **Expected:** Theme persists (stays dark if you set dark)

---

## ?? Common Issues & Fixes

### Issue 1: Button Still Not Working
**Symptoms:** Nothing happens when clicking buttons

**Diagnosis:**
```javascript
// In browser console (F12):
Blazor
// Should return an object, not "undefined"
```

**Fix:**
- Clear browser cache (Ctrl+F5)
- Restart Docker container
- Verify latest code deployed

---

### Issue 2: Form Doesn't Submit
**Symptoms:** Click Save, nothing happens

**Check:**
1. **Browser Console** (F12) - Any errors?
2. **Network tab** - Is request being sent?
3. **Validation** - Are there validation errors?

**Fix:**
```javascript
// Check form validity
document.querySelector('form').reportValidity()
```

---

### Issue 3: Data Not Saving
**Symptoms:** Form submits but data disappears

**Check Database:**
```sh
docker exec -it tradeflow-db /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "YourPassword" -C -Q "SELECT COUNT(*) FROM TradeFlowFSM.dbo.Customers"
```

**Check Logs:**
```sh
docker logs tradeflow-webui --tail 50
```

---

### Issue 4: Validation Not Showing
**Symptoms:** Can submit invalid data

**Check:**
- Are `<ValidationMessage>` components in the form?
- Is `<DataAnnotationsValidator />` present?
- Are model properties decorated with validation attributes?

---

## ?? Quick Checklist

Use this for rapid testing:

- [ ] Navigate to /customers
- [ ] Click "Add Customer" button
- [ ] Form loads correctly
- [ ] Fill in test data
- [ ] Click "Save" button
- [ ] Customer appears in list
- [ ] Click customer to view details
- [ ] Edit customer works
- [ ] Validation shows for empty required fields
- [ ] Dark mode toggle works
- [ ] No console errors

---

## ?? Browser Console Tests

Open DevTools (F12) and run these:

```javascript
// 1. Check Blazor is loaded
Blazor
// Should return: {platform: 'server', ...}

// 2. Check WebSocket connection
// Go to Network tab > WS filter
// Should see: "_blazor?id=..." with status 101

// 3. Check for errors
// Console tab should have NO red errors

// 4. Test button click manually
document.querySelector('[data-test="add-customer"]')?.click()
// Or just click the actual button
```

---

## ?? Expected Results Summary

| Test | Expected Result | Time |
|------|----------------|------|
| Add Customer button | Navigates to form | Instant |
| Form load | All fields visible | <1s |
| Save customer | Success message | <2s |
| Customer in list | Appears in search | <1s |
| Edit customer | Loads form with data | <1s |
| Validation | Shows error messages | Instant |
| Dark mode | Theme changes | Instant |

---

## ?? If Everything Fails

### Nuclear Option: Complete Reset

```sh
# On server
cd ~/OneManVan

# Pull latest code
git pull origin master

# Rebuild everything
docker-compose down
docker-compose build --no-cache
docker-compose up -d

# Check logs
docker-compose logs -f webui
```

### Check Deployment

```sh
# Verify latest commit
git log --oneline -1

# Should show: "Fix critical Blazor buttons issue..."
```

---

## ? Success Criteria

**All tests pass when:**
1. ? All buttons respond to clicks
2. ? Forms submit successfully
3. ? Data saves to database
4. ? Validation works correctly
5. ? No console errors
6. ? WebSocket connection active
7. ? Dark mode toggles

---

## ?? Test Report Template

```
Date: _______________
Tester: _______________
Environment: Production / Staging / Local

Test Results:
[ ] Add Customer button works
[ ] Customer form saves
[ ] Customer appears in list
[ ] Edit customer works
[ ] Validation displays
[ ] Dark mode toggles
[ ] No console errors

Issues Found:
_______________________________________________
_______________________________________________

Browser: _______________
Server: _______________
```

---

## ?? Next Steps After Successful Tests

1. **Test other "Add" buttons:**
   - Add Job
   - Add Invoice
   - Add Estimate
   - Add Product

2. **Test other interactive features:**
   - Modals opening
   - Dropdowns working
   - Calendar interactions
   - Export buttons

3. **Load testing (optional):**
   - Create 10+ customers rapidly
   - Check performance

---

**Ready to test? Start with Test 1 and work through the checklist!** ??
