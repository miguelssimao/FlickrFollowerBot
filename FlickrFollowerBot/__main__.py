import json
import os
import subprocess
import tkinter as tk
from tkinter import filedialog, messagebox, ttk

import darkdetect


class FlickrFollowerBot:
    def __init__(self):
        self.root = tk.Tk()
        self.root.title("FlickrFollowerBot")
        self.root.geometry("980x780")

        self.dir = os.path.dirname(os.path.realpath(__file__))
        self.theme_file = os.path.join(self.dir, "azure.tcl")
        self.icon_file = os.path.join(self.dir, "assets/icon.ico")
        self.config_file = os.path.join(self.dir, "FlickrFollowerBot.json")

        self.root.tk.call("source", self.theme_file)
        self.root.tk.call("set_theme", "light")

        if darkdetect.isDark():
            self.root.tk.call("set_theme", "dark")

        self.config = self.load_config()
        self.bot_tasks = []

        self.create_ui()
        self.center_window(self.root)

        self.root.resizable(False, True)
        self.root.iconbitmap(self.icon_file)

    def load_config(self):
        """Load configuration from JSON file."""
        if os.path.exists(self.config_file):
            try:
                with open(self.config_file, "r") as f:
                    config = json.load(f)

                    for key in [
                        "ChromeBinaryLocation",
                        "ChromeDriverLocation",
                        "BotUserEmail",
                        "BotUserPassword",
                    ]:
                        if key in config and config[key] is None:
                            config[key] = ""

                    return config
            except Exception:
                pass

        return {
            "ChromeBinaryLocation": "",
            "ChromeDriverLocation": "",
            "BotUserEmail": "",
            "BotUserPassword": "",
        }

    def save_config(self):
        """Save configuration to JSON file."""
        with open(self.config_file, "w") as f:
            json.dump(self.config, f, indent=4)

    def center_window(self, window):
        """Center the window on screen."""
        window.update_idletasks()
        width = window.winfo_width()
        height = window.winfo_height()
        x = (window.winfo_screenwidth() // 2) - (width // 2)
        y = (window.winfo_screenheight() // 2) - (height // 2)
        window.geometry(f"+{x}+{y}")

    def create_ui(self):
        """Create the main UI components."""
        main_frame = ttk.Frame(self.root)
        main_frame.pack(fill=tk.BOTH, expand=True, padx=10, pady=10)

        top_frame = ttk.Frame(main_frame)
        top_frame.pack(fill=tk.X)

        title_font = ("Arial", 10, "bold")

        menu_label = ttk.Label(top_frame, text="Menu", font=title_font)
        menu_label.pack(side=tk.LEFT)

        bot_tasks_label = ttk.Label(top_frame, text="Bot Tasks", font=title_font)
        bot_tasks_label.pack(side=tk.LEFT, padx=(237, 0))

        clear_label = ttk.Label(top_frame, text="Clear", font=("Arial", 10))
        clear_label.pack(side=tk.RIGHT, padx=(0, 2))
        clear_label.bind("<Button-1>", self.clear_bot_tasks)

        middle_frame = ttk.Frame(main_frame)
        middle_frame.pack(fill=tk.BOTH, expand=True, pady=(0, 5))

        menu_items = [
            "DetectContactsFromPhoto",
            "DetectContactsFollowBack",
            "DoContactsFollow",
            "",
            "DetectContactsUnfollowBack",
            "DoContactsInactiveUnfollow",
            "DoContactsUnfollow",
            "",
            "DetectRecentContactPhotos",
            "SearchKeywords",
            "DoContactsFav",
            "DoPhotosFav",
            "",
            "Save",
            "Loop",
            "Wait",
        ]
        
        listbox_height = min(len(menu_items), 16)

        menu_frame = ttk.LabelFrame(middle_frame, text="-")
        menu_frame.pack(side=tk.LEFT, fill=tk.BOTH, expand=True, padx=(0, 5))

        self.menu_listbox = tk.Listbox(
            menu_frame,
            font=("Arial", 10),
            selectmode=tk.SINGLE,
            borderwidth=0,
            highlightthickness=0,
            height=listbox_height,
        )
        self.menu_listbox.pack(fill=tk.BOTH, expand=True, padx=5, pady=5)

        for item in menu_items:
            self.menu_listbox.insert(tk.END, item)

        self.menu_listbox.bind("<Double-Button-1>", self.add_to_bot_tasks)
        self.menu_listbox.bind("<Return>", self.add_to_bot_tasks)

        tasks_frame = ttk.LabelFrame(middle_frame, text="-")
        tasks_frame.pack(side=tk.RIGHT, fill=tk.BOTH, expand=True, padx=(5, 0))

        self.bot_tasks_text = tk.Text(
            tasks_frame,
            font=("Arial", 10),
            borderwidth=0,
            highlightthickness=0,
            height=listbox_height,
        )
        self.bot_tasks_text.pack(fill=tk.BOTH, expand=True, padx=5, pady=5)

        self.bot_tasks_text.bind("<<Modified>>", self.on_text_modified)

        chrome_folder_frame = ttk.Frame(main_frame, padding=(0, 5))
        chrome_folder_frame.pack(fill=tk.X, pady=(0, 10))

        folder_label = ttk.Label(
            chrome_folder_frame, text="Chrome.exe", font=title_font
        )
        folder_label.pack(anchor=tk.W, pady=2)

        chrome_path_frame = ttk.Frame(chrome_folder_frame)
        chrome_path_frame.pack(fill=tk.X, pady=2)

        self.chrome_path_entry = ttk.Entry(chrome_path_frame)
        self.chrome_path_entry.pack(
            side=tk.LEFT,
            fill=tk.X,
            expand=True,
            padx=(0, 5),
            ipady=2,
        )

        if self.config["ChromeBinaryLocation"].lower() == "chrome-win64\chrome.exe":
            c_binary = os.path.join(self.dir, self.config["ChromeBinaryLocation"])
            self.chrome_path_entry.insert(0, c_binary)
        else:
            self.chrome_path_entry.insert(0, self.config["ChromeBinaryLocation"])

        chrome_browse_btn = ttk.Button(
            chrome_path_frame,
            text="...",
            width=3,
            padding=6,
            command=lambda: self.select_path("chrome"),
        )
        chrome_browse_btn.pack(side=tk.RIGHT)

        driver_label = ttk.Label(
            chrome_folder_frame, text="ChromeDriver.exe's location", font=title_font
        )
        driver_label.pack(anchor=tk.W, pady=(7, 2))

        driver_path_frame = ttk.Frame(chrome_folder_frame)
        driver_path_frame.pack(fill=tk.X, pady=2)

        self.chromedriver_path_entry = ttk.Entry(driver_path_frame)
        self.chromedriver_path_entry.pack(
            side=tk.LEFT,
            fill=tk.X,
            expand=True,
            padx=(0, 5),
            ipady=2,
        )

        if self.config["ChromeDriverLocation"].lower() == "chrome-win64":
            c_driver = os.path.join(self.dir, self.config["ChromeDriverLocation"])
            self.chromedriver_path_entry.insert(0, c_driver)
        else:
            self.chromedriver_path_entry.insert(0, self.config["ChromeDriverLocation"])

        driver_browse_btn = ttk.Button(
            driver_path_frame,
            text="...",
            width=3,
            padding=6,
            command=lambda: self.select_path("chromedriver"),
        )
        driver_browse_btn.pack(side=tk.RIGHT)

        login_label = ttk.Label(main_frame, text="Flickr", font=title_font)
        login_label.pack(anchor=tk.W, pady=(0, 5))

        login_frame = ttk.Frame(main_frame)
        login_frame.pack(fill=tk.X)

        login_fields_frame = ttk.Frame(login_frame)
        login_fields_frame.pack(side=tk.LEFT, fill=tk.BOTH, expand=True, padx=(0, 10))

        go_frame = ttk.Frame(login_frame)
        go_frame.pack(side=tk.RIGHT, fill=tk.Y)

        self.email_entry = ttk.Entry(login_fields_frame)
        self.email_entry.pack(fill=tk.X, pady=(0, 5), ipady=2)

        self.email_placeholder = "Email address"
        email = self.config["BotUserEmail"]
        if email and email != "null":
            self.email_entry.insert(0, email)
        else:
            self.email_entry.insert(0, self.email_placeholder)
            self.email_entry.config(foreground="gray")
        self.email_entry.bind("<FocusIn>", self.on_email_focus_in)
        self.email_entry.bind("<FocusOut>", self.on_email_focus_out)

        self.password_entry = ttk.Entry(login_fields_frame)
        self.password_entry.pack(fill=tk.X, ipady=2)

        self.password_placeholder = "********"
        password = self.config["BotUserPassword"]
        if password and password != "null":
            self.password_entry.config(show="*")
            self.password_entry.insert(0, password)
        else:
            self.password_entry.insert(0, self.password_placeholder)
            self.password_entry.config(foreground="gray")
        self.password_entry.bind("<FocusIn>", self.on_password_focus_in)
        self.password_entry.bind("<FocusOut>", self.on_password_focus_out)

        go_btn = ttk.Button(go_frame, text="Go", width=10, command=self.run_bot)
        go_btn.pack(fill=tk.BOTH, expand=True, ipady=4)

    def on_email_focus_in(self, event):
        """Handle focus in event for email field."""
        if self.email_entry.get() == self.email_placeholder:
            self.email_entry.delete(0, tk.END)
            self.email_entry.config(foreground="")

    def on_email_focus_out(self, event):
        """Handle focus out event for email field."""
        if not self.email_entry.get():
            self.email_entry.insert(0, self.email_placeholder)
            self.email_entry.config(foreground="gray")

    def on_password_focus_in(self, event):
        """Handle focus in event for password field."""
        if self.password_entry.get() == self.password_placeholder:
            self.password_entry.delete(0, tk.END)
            self.password_entry.config(foreground="", show="*")

    def on_password_focus_out(self, event):
        """Handle focus out event for password field."""
        if not self.password_entry.get():
            self.password_entry.insert(0, self.password_placeholder)
            self.password_entry.config(foreground="gray", show="")

    def on_text_modified(self, event=None):
        """Handle text modification in bot_tasks_text and update bot_tasks list."""
        self.bot_tasks_text.edit_modified(False)
        text_content = self.bot_tasks_text.get("1.0", "end-1c")
        self.bot_tasks = [line for line in text_content.split("\n") if line.strip()]

    def select_path(self, path_type):
        """Open a file dialog to select a path."""
        if path_type == "chromedriver":
            path = filedialog.askdirectory(title="Select ChromeDriver.exe folder")
            if path:
                self.chromedriver_path_entry.delete(0, tk.END)
                self.chromedriver_path_entry.insert(0, path)
        else:
            path = filedialog.askopenfilename(
                title="Select Chrome.exe executable file",
                filetypes=[("Executable files", "*.exe")],
            )
            if path:
                self.chrome_path_entry.delete(0, tk.END)
                self.chrome_path_entry.insert(0, path)

    def add_to_bot_tasks(self, event):
        """Add selected menu item to bot tasks."""
        selected_indices = self.menu_listbox.curselection()
        if not selected_indices:
            return

        selected_item = self.menu_listbox.get(selected_indices[0])

        if selected_item == "":
            return
        elif selected_item == "DetectContactsFromPhoto":
            self.open_url_dialog()
        else:
            self.bot_tasks.append(selected_item)
            self.update_bot_tasks_display()

    def clear_bot_tasks(self, event):
        """Clears all bot tasks."""
        self.bot_tasks = []
        self.bot_tasks_text.delete(1.0, tk.END)

    def open_url_dialog(self):
        """Open a dialog to specify an URL for DetectContactsFromPhoto."""
        dialog = tk.Toplevel(self.root)
        dialog.title("DetectContactsFromPhoto")
        dialog.geometry("500x175")
        dialog.transient(self.root)
        dialog.grab_set()

        dialog_frame = ttk.Frame(dialog, padding=10)
        dialog_frame.pack(fill=tk.BOTH, expand=True)

        label = ttk.Label(dialog_frame, text="Enter the URL for a Flickr's photo")
        label.pack(pady=10)

        url_entry = ttk.Entry(dialog_frame, width=50)
        url_entry.pack(pady=10)
        url_entry.focus_set()

        def submit_url():
            url = url_entry.get().strip()
            if url:
                task = f"DetectContactsFromPhoto={url}"
                self.bot_tasks.append(task)
                self.update_bot_tasks_display()
            dialog.destroy()

        url_entry.bind("<Return>", lambda event: submit_url())

        submit_btn = ttk.Button(dialog_frame, text="Add", command=submit_url)
        submit_btn.pack(pady=10)

        self.center_window(dialog)
        dialog.iconbitmap(self.icon_file)

    def update_bot_tasks_display(self):
        """Update the bot tasks display."""
        self.bot_tasks_text.unbind("<<Modified>>")

        self.bot_tasks_text.delete("1.0", tk.END)
        for task in self.bot_tasks:
            self.bot_tasks_text.insert(tk.END, f"{task}\n")

        self.bot_tasks_text.bind("<<Modified>>", self.on_text_modified)

    def validate_inputs(self):
        """Validate all input fields before running the bot."""
        missing_fields = []

        if not self.bot_tasks:
            missing_fields.append("Bot Tasks")

        email = self.email_entry.get().strip()
        if not email or email == self.email_placeholder:
            missing_fields.append("Flickr Email")

        password = self.password_entry.get().strip()
        if not password or password == self.password_placeholder:
            missing_fields.append("Flickr Password")

        if not self.chrome_path_entry.get().strip():
            missing_fields.append("Chrome.exe path")

        if not self.chromedriver_path_entry.get().strip():
            missing_fields.append("ChromeDriver.exe path")

        if missing_fields:
            message = "The following fields are empty:\n\n- " + "\n- ".join(
                missing_fields
            )
            messagebox.showwarning("Missing Information", message)
            return False

        return True

    def run_bot(self):
        """Save and run."""
        if not self.validate_inputs():
            return

        c_binary_location = self.chrome_path_entry.get().replace("/", "\\")
        c_driver_location = self.chromedriver_path_entry.get().replace("/", "\\")

        self.config["ChromeBinaryLocation"] = c_binary_location
        self.config["ChromeDriverLocation"] = c_driver_location

        email = self.email_entry.get()
        if email != self.email_placeholder:
            self.config["BotUserEmail"] = email

        password = self.password_entry.get()
        if password != self.password_placeholder:
            self.config["BotUserPassword"] = password

        self.save_config()

        tasks_string = ",".join(self.bot_tasks)
        cmd = f"dotnet run BotTasks={tasks_string}"

        try:
            self.root.iconify()
            subprocess.Popen(f'start cmd /k "{cmd}"', shell=True)
        except Exception as e:
            messagebox.showerror("Error", f"Failed to run bot: {str(e)}")

    def run(self):
        """Start the mainloop."""
        self.root.mainloop()


if __name__ == "__main__":
    app = FlickrFollowerBot()
    app.run()
