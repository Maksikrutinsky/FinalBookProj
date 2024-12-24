using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using BookProject.Models;

public class UserManagementController : Controller
{
    private readonly EBookLibraryEntities _context;

    public UserManagementController()
    {
        _context = new EBookLibraryEntities();
    }

    public async Task<ActionResult> Index()
    {
        var userId = Session["UserId"];
        if (userId == null) return RedirectToAction("Login", "Account");

        int currentUserId = Convert.ToInt32(userId);
        var isAdmin = await _context.Users.Where(u => u.UserId == currentUserId).Select(u => u.IsAdmin).FirstOrDefaultAsync() ?? false;
        if (!isAdmin) return RedirectToAction("Index", "Home");

        var users = await _context.Users.ToListAsync();
        return View(users);
    }

    public async Task<ActionResult> Edit(int id)
    {
        var userId = Session["UserId"];
        if (userId == null) return RedirectToAction("Login", "Account");

        int currentUserId = Convert.ToInt32(userId);
        var isAdmin = await _context.Users.Where(u => u.UserId == currentUserId).Select(u => u.IsAdmin).FirstOrDefaultAsync() ?? false;
        if (!isAdmin) return RedirectToAction("Index", "Home");

        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return HttpNotFound();
        }
        return View(user);
    }

    [HttpPost]
    public async Task<ActionResult> Edit(User user)
    {
        var userId = Session["UserId"];
        if (userId == null) return RedirectToAction("Login", "Account");

        int currentUserId = Convert.ToInt32(userId);
        var isAdmin = await _context.Users.Where(u => u.UserId == currentUserId).Select(u => u.IsAdmin).FirstOrDefaultAsync() ?? false;
        if (!isAdmin) return RedirectToAction("Index", "Home");

        if (ModelState.IsValid)
        {
            var existingUser = await _context.Users.FindAsync(user.UserId);
            if (existingUser == null) return HttpNotFound();

            existingUser.Username = user.Username;
            existingUser.Email = user.Email;
            existingUser.IsAdmin = user.IsAdmin;
            existingUser.Address = user.Address;
            existingUser.City = user.City;
            existingUser.ZipCode = user.ZipCode;
            existingUser.PhoneNumber = user.PhoneNumber;

            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }
        return View(user);
    }

    [HttpPost]
    public async Task<ActionResult> Delete(int id)
    {
        var userId = Session["UserId"];
        if (userId == null) return RedirectToAction("Login", "Account");

        int currentUserId = Convert.ToInt32(userId);
        var isAdmin = await _context.Users.Where(u => u.UserId == currentUserId).Select(u => u.IsAdmin).FirstOrDefaultAsync() ?? false;
        if (!isAdmin) return RedirectToAction("Index", "Home");

        var user = await _context.Users.FindAsync(id);
        if (user == null) return HttpNotFound();

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        return RedirectToAction("Index");
    }
}