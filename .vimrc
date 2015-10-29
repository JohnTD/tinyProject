""""""""""""""""""""""""""""""""""""""""""
" => General
""""""""""""""""""""""""""""""""""""""""""
" Friendly to fish shell
set shell=bash

" Set to auto read when a file is changed from the outside
set autoread

" Do not add comment automatically under a comment line
" mkdir -p ~/.vim/after/ftplugin/
" touch c.vim cpp.vim vim.vim
" Edit with setl comments=sO:*\ -,mO:*\ \ ,exO:*/,s1:/*,mb:*,ex:*/,f://

" With a map leader it's possible to do extra key combinations
let mapleader = ","
let g:mapleader = ","

" Fast saving
nmap <Leader>w :w!<CR>

" Fast quit
nmap <Leader>q :q<CR>

" Fast ESC
imap jj <ESC>


""""""""""""""""""""""""""""""""""""""""""
" => VIM user interface
""""""""""""""""""""""""""""""""""""""""""
" Turn on the Wild menu
set wildmenu

" Ignore compiled files
set wildignore=*.swp,*.o,*.pyc

" A buffer becomes hidden when it's abandoned
set hid

set magic " Enable magic matching
set showmatch " Show matching bracets
set hlsearch " Highlight search things
set smartcase " Ignore case when searching
set ignorecase
set incsearch 

set cursorline
set cursorcolumn
set nu
set ruler


"""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""
" => Text, tab and indent related
"""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""
" Use spaces instead of tabs
set expandtab

" Be smart when using tabs ;)
set smarttab

" 1 tab == 4 spaces
set shiftwidth=4
set tabstop=4

" Linebreak on 500 characters
set lbr
set tw=500

set ai "Auto indent
set si "Smart indent
set wrap "Wrap lines


""""""""""""""""""""""""""""""""""""""""""
" => Copy, Cut and Paste
"""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""
"must install vim-gnome before"
vmap <C-c> "+y
vmap <C-x> "+d
nmap <C-p> "+p
vmap <C-p> c<ESC>"+p
imap <C-p> <C-r><C-o>+
set clipboard+=unnamed
set selection=exclusive


""""""""""""""""""""""""""""""""""""""""""
" => Tab, Window and Define moving
""""""""""""""""""""""""""""""""""""""""""
nnoremap <Leader>1 1gt
nnoremap <Leader>2 2gt
nnoremap <Leader>3 3gt
nnoremap <Leader>4 4gt
nnoremap <Leader>5 5gt
nnoremap <Leader>6 6gt
nnoremap <Leader>7 7gt
nnoremap <Leader>8 8gt
nnoremap <Leader>9 9gt
nnoremap <Leader>0 :tablast

" Smart way to move between windows
map <C-j> <C-W>j
map <C-k> <C-W>k
map <C-h> <C-W>h
map <C-l> <C-W>l

" Return to last edit position when opening files (You want this!)
autocmd BufReadPost *
     \ if line("'\"") > 0 && line("'\"") <= line("$") |
     \   exe "normal! g`\"" |
     \ endif

" Go to define
" Must run '!ctags -R' before use it
nnoremap <Leader>gd g<c-]>
nnoremap <Leader>gb <c-o>


""""""""""""""""""""""""""""""""""""""""""
" => Colors and Fonts
""""""""""""""""""""""""""""""""""""""""""
" http://bytefluent.com/vivify/-- online test "

" Enable syntax highlighting
syntax enable

colorscheme jellybeans
hi Normal ctermfg=white
set t_Co=256

" Set utf8 as standard encoding and en_US as the standard language
set encoding=utf8


""""""""""""""""""""""""""""""""""""""""""
" => VIM Plugins
""""""""""""""""""""""""""""""""""""""""""

" => NERDTree and Tagbar"
map <Leader>nt :NERDTreeToggle<CR>
map <Leader>tb :TagbarToggle<CR>

let NERDTreeWinSize=22
let g:tagbar_width=20

" Auto close nerdtree if the only window left open is a NERDTree
autocmd bufenter * if (winnr("$") == 1 && exists("b:NERDTreeType") &&b:NERDTreeType == "primary") | q | endif


" => Easymotion settings
map <Leader>l <Plug>(easymotion-lineforward)
map <Leader>j <Plug>(easymotion-j)
map <Leader>k <Plug>(easymotion-k)
map <Leader>h <Plug>(easymotion-linebackward)
let g:EasyMotion_startofline = 0
map  / <Plug>(easymotion-sn)
omap / <Plug>(easymotion-tn)


" => Ctrlsf.vim
" Must install silversearcher-ag Before-- search in project"
map <Leader>sp :CtrlSF<CR>


" => Air-line
"https://github.com/powerline/fonts
" git clone https://github.com/powerline/fonts.git
" ./install.sh
" set terminal font->Profiles->Default(Edit)->General->Font 
set guifont=DejaVu\ Sans\ Mono\ for\ Powerline
set laststatus=2
let g:airline_powerline_fonts=1
let g:airline#extensions#tabline#enabled = 1
let g:airline_theme='jellybeans'


" => Ctrlspace
set nocompatible
set hidden


" => Rainbow Parentheses
let g:rbpt_colorpairs = [
    \ ['brown',       'RoyalBlue3'],
    \ ['Darkblue',    'SeaGreen3'],
    \ ['darkgray',    'DarkOrchid3'],
    \ ['darkgreen',   'firebrick3'],
    \ ['darkcyan',    'RoyalBlue3'],
    \ ['darkred',     'SeaGreen3'],
    \ ['darkmagenta', 'DarkOrchid3'],
    \ ['brown',       'firebrick3'],
    \ ['gray',        'RoyalBlue3'],
    \ ['black',       'SeaGreen3'],
    \ ['darkmagenta', 'DarkOrchid3'],
    \ ['Darkblue',    'firebrick3'],
    \ ['darkgreen',   'RoyalBlue3'],
    \ ['darkcyan',    'SeaGreen3'],
    \ ['darkred',     'DarkOrchid3'],
    \ ['red',         'firebrick3'],
    \ ]

let g:rbpt_max = 16
let g:rbpt_loadcmd_toggle = 0
au VimEnter * RainbowParenthesesToggle
au Syntax * RainbowParenthesesLoadRound
au Syntax * RainbowParenthesesLoadSquare
au Syntax * RainbowParenthesesLoadBraces


" => YouCompleteMe
inoremap <expr> <CR> pumvisible() ? "\<C-y>" : "\<CR>"  "enter selection
let g:ycm_seed_identifiers_with_syntax=1   "keyword complete
let g:ycm_complete_in_strings=1  "complete in strings
let g:ycm_complete_in_comments = 1  "also complete in comments
set completeopt=longest,menu   "complete menu setting

let g:UltiSnipsExpandTrigger="<F2>"
let g:UltiSnipsJumpForwardTrigger="<c-b>"
let g:UltiSnipsJumpBackwardTrigger="<c-z>"


""""""""""""""""""""""""""""""""""""""""""
" => Vundle settings
""""""""""""""""""""""""""""""""""""""""""
set nocompatible
filetype off

set rtp+=~/.vim/bundle/Vundle.vim
call vundle#begin()

" Let Vundle manage itself
" Must run git clone http://github.com/gmarik/vundle.git ~/.vim/bundle/vundle
Plugin 'gmarik/Vundle.vim'

" Easily move, very useful
Plugin 'easymotion/vim-easymotion'

" YouCompleteMe very useful in complete
Plugin 'Valloric/YouCompleteMe'

" Xptemlate make insert code easily
Plugin 'drmingdrmer/xptemplate'

" C++ complete
Plugin 'Rip-Rip/clang_complete'

" Python complete
Plugin 'davidhalter/jedi'

" Search in Project
Plugin 'dyng/ctrlsf.vim'

" Language check
Plugin 'scrooloose/syntastic'

" NerdTree shows folders and files
Plugin 'scrooloose/nerdtree'

" Tagbar shows functions and variable
Plugin 'majutsushi/tagbar'

" Buffer move 
Plugin 'szw/vim-ctrlspace'

" C++ Highlight
Plugin 'octol/vim-cpp-enhanced-highlight'

" Python Highlight
Plugin 'python-syntax'

" JS Highlight
Plugin 'pangloss/vim-javascript'

" Jquery Highlight
Plugin 'nono/jquery.vim'

" Jinja2 Highlight
Plugin 'Glench/Vim-Jinja2-Syntax'

" Nginx Highlight
Plugin 'evanmiller/nginx-vim-syntax'

" Css Highlight
Plugin 'JulesWang/css.vim'

" Less Highlight
Plugin 'groenewege/vim-less'

" Grunt Highlight
Plugin 'mklabs/grunt.vim'

" Code comment
Plugin 'scrooloose/nerdcommenter' 

" Add or delete '" easily
Plugin 'tpope/vim-surround'

" Automatic quotes, parenthesis and  brackets
Plugin 'Raimondi/delimitMate'

" visually select increasingly or decreasingly between quotes, parenthesis and  brackets
Plugin 'terryma/vim-expand-region'

" Make quotes, parenthesis and brackets beautiful
Plugin 'kien/rainbow_parentheses.vim'

" Expands function of %
Plugin 'vim-scripts/matchit.zip'

" Multiple cursors select
Plugin 'terryma/vim-multiple-cursors'

" Airline
Plugin 'bling/vim-airline'

" Git
Plugin 'tpope/vim-fugitive'

call vundle#end()
filetype plugin indent on
